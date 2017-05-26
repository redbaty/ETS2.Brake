using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Threading;
using EasyHook;
using Overlay.Hook;
using Overlay.Interface;

namespace Overlay
{
    public sealed class OverlayProcess : IDisposable
    {
        /// <summary>
        /// Must be null to allow a random channel name to be generated
        /// </summary>
        private readonly string _channelName;
        private readonly OverlayInterface _serverInterface;

        private Process Process { get; set; }

        /// <summary>
        /// Prepares capturing in the target process. Note that the process must not already be hooked, and must have a <see cref="Process.MainWindowHandle"/>.
        /// </summary>
        /// <param name="process">The process to inject into</param>
        /// <exception cref="ProcessHasNoWindowHandleException">Thrown if the <paramref name="process"/> does not have a window handle. This could mean that the process does not have a UI, or that the process has not yet finished starting.</exception>
        /// <exception cref="ProcessAlreadyHookedException">Thrown if the <paramref name="process"/> is already hooked</exception>
        /// <exception cref="InjectionFailedException">Thrown if the injection failed - see the InnerException for more details.</exception>
        /// <remarks>The target process will have its main window brought to the foreground after successful injection.</remarks>
        public OverlayProcess(Process process, OverlayConfig config, OverlayInterface overlayInterface)
        {
            // If the process doesn't have a mainwindowhandle yet, skip it (we need to be able to get the hwnd to set foreground etc)
            if (process.MainWindowHandle == IntPtr.Zero)
            {
                throw new ProcessHasNoWindowHandleException();
            }

            // Skip if the process is already hooked (and we want to hook multiple applications)
            if (HookManager.IsHooked(process.Id))
            {
                throw new ProcessAlreadyHookedException();
            }

            _serverInterface = overlayInterface;
            //_serverInterface = new OverlayInterface() { ProcessId = process.Id };

            // Initialise the IPC server (with our instance of _serverInterface)
            RemoteHooking.IpcCreateServer(
                ref _channelName,
                WellKnownObjectMode.Singleton,
                _serverInterface);

            try
            {
                RemoteHooking.Inject(
                    process.Id,
                    InjectionOptions.Default,
                    typeof(OverlayInterface).Assembly.Location,
                    typeof(OverlayInterface).Assembly.Location,
                    _channelName,
                    config
                );
            }
            catch (Exception e)
            {
                throw new InjectionFailedException(e);
            }

            HookManager.AddHookedProcess(process.Id);

            Process = process;

            // Ensure the target process is in the foreground,
            // this prevents an issue where the target app appears to be in 
            // the foreground but does not receive any user inputs.
            // Note: the first Alt+Tab out of the target application after injection
            //       may still be an issue - switching between windowed and 
            //       fullscreen fixes the issue however (see ScreenshotInjection.cs for another option)
            BringProcessWindowToFront();
        }

        public OverlayInterface OverlayInterface => _serverInterface;

        ~OverlayProcess()
        {
            Dispose(false);
        }


        #region Private methods

        /// <summary>
        /// Bring the target window to the front and wait for it to be visible
        /// </summary>
        /// <remarks>If the window does not come to the front within approx. 30 seconds an exception is raised</remarks>
        public void BringProcessWindowToFront()
        {
            if (Process == null)
                return;
            var handle = Process.MainWindowHandle;
            var i = 0;

            while (!NativeMethods.IsWindowInForeground(handle))
            {
                if (i == 0)
                {
                    // Initial sleep if target window is not in foreground - just to let things settle
                    Thread.Sleep(250);
                }

                if (NativeMethods.IsIconic(handle))
                {
                    // Minimized so send restore
                    NativeMethods.ShowWindow(handle, NativeMethods.WindowShowStyle.Restore);
                }
                else
                {
                    // Already Maximized or Restored so just bring to front
                    NativeMethods.SetForegroundWindow(handle);
                }
                Thread.Sleep(250);

                // Check if the target process main window is now in the foreground
                if (NativeMethods.IsWindowInForeground(handle))
                {
                    // Leave enough time for screen to redraw
                    Thread.Sleep(1000);
                    return;
                }

                // Prevent an infinite loop
                if (i > 120) // about 30secs
                {
                    throw new Exception("Could not set process window to the foreground");
                }
                i++;
            }
        }

        #endregion

        #region IDispose

        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Disconnect the IPC (which causes the remote entry point to exit)
                    _serverInterface.Disconnect();
                }

                _disposed = true;
            }
        }

        #endregion
    }
}
