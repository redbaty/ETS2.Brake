using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Threading;
using EasyHook;
using Overlay.Interface;
using Overlay.Toolkit;

namespace Overlay.Hook
{
    public abstract class BaseDxHook : Component, IDXHook
    {
        protected BaseDxHook(OverlayInterface ssInterface)
        {
            Interface = ssInterface;
        }

        ~BaseDxHook()
        {
            Dispose(false);
        }

        

        int _processId;

        protected int ProcessId
        {
            get
            {
                if (_processId == 0)
                {
                    _processId = RemoteHooking.GetCurrentProcessId();
                }
                return _processId;
            }
        }

        protected virtual string HookName => "BaseDXHook";

        protected void DebugMessage(string message)
        {
#if DEBUG
            try
            {
                Interface.Message(MessageType.Debug, HookName + ": " + message);
            }
            catch (RemotingException)
            {
                // Ignore remoting exceptions
            }
#endif
        }

        protected IntPtr[] GetVTblAddresses(IntPtr pointer, int numberOfMethods)
        {
            return GetVTblAddresses(pointer, 0, numberOfMethods);
        }

        protected IntPtr[] GetVTblAddresses(IntPtr pointer, int startIndex, int numberOfMethods)
        {
            var vtblAddresses = new List<IntPtr>();

            var vTable = Marshal.ReadIntPtr(pointer);
            for (var i = startIndex; i < startIndex + numberOfMethods; i++)
                vtblAddresses.Add(Marshal.ReadIntPtr(vTable,
                    i * IntPtr.Size)); // using IntPtr.Size allows us to support both 32 and 64-bit processes

            return vtblAddresses.ToArray();
        }

        protected static void CopyStream(Stream input, Stream output)
        {
            var bufferSize = 32768;
            var buffer = new byte[bufferSize];
            while (true)
            {
                var read = input.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    return;
                }
                output.Write(buffer, 0, read);
            }
        }

        /// <summary>
        /// Reads data from a stream until the end is reached. The
        /// data is returned as a byte array. An IOException is
        /// thrown if any of the underlying IO calls fail.
        /// </summary>
        /// <param name="stream">The stream to read data from</param>
        protected static byte[] ReadFullStream(Stream stream)
        {
            var memoryStream = stream as MemoryStream;
            if (memoryStream != null)
            {
                return memoryStream.ToArray();
            }
            var buffer = new byte[32768];
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read > 0)
                        ms.Write(buffer, 0, read);
                    if (read < buffer.Length)
                    {
                        return ms.ToArray();
                    }
                }
            }
        }

        #region IDXHook Members

        public OverlayInterface Interface { get; set; }

        public OverlayConfig Config { get; set; }

        protected readonly List<Hook> Hooks = new List<Hook>();
        public abstract void Hook();

        public abstract void Cleanup();

        #endregion

        #region IDispose Implementation

        protected override void Dispose(bool disposeManagedResources)
        {
            // Only clean up managed objects if disposing (i.e. not called from destructor)
            if (disposeManagedResources)
            {
                try
                {
                    Cleanup();
                }
                catch
                {
                }

                try
                {
                    // Uninstall Hooks
                    if (Hooks.Count > 0)
                    {
                        // First disable the hook (by excluding all threads) and wait long enough to ensure that all hooks are not active
                        foreach (var hook in Hooks)
                        {
                            // Lets ensure that no threads will be intercepted again
                            hook.Deactivate();
                        }

                        Thread.Sleep(100);

                        // Now we can dispose of the hooks (which triggers the removal of the hook)
                        foreach (var hook in Hooks)
                        {
                            hook.Dispose();
                        }

                        Hooks.Clear();
                    }
                }
                catch
                {
                }
            }

            base.Dispose(disposeManagedResources);
        }

        #endregion
    }
}