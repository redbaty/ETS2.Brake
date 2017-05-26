using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Overlay.Elements;
using Overlay.Hook.DX9;
using Overlay.Interface;
using SharpDX;
using SharpDX.Direct3D9;
using SharpDX.Mathematics.Interop;
using Color = System.Drawing.Color;
using Font = SharpDX.Direct3D9.Font;
using Point = System.Drawing.Point;

//using SlimDX.Direct3D9;

namespace Overlay.Hook
{
    public class DxHookD3D9 : BaseDxHook
    {
        public DxHookD3D9(OverlayInterface ssInterface)
            : base(ssInterface)
        {
        }

        private Hook<Direct3D9DeviceEndSceneDelegate> _direct3DDeviceEndSceneHook;
        private Hook<Direct3D9DeviceResetDelegate> _direct3DDeviceResetHook;
        private Hook<Direct3D9DevicePresentDelegate> _direct3DDevicePresentHook;
        private Hook<Direct3D9DeviceExPresentExDelegate> _direct3DDeviceExPresentExHook;
        private readonly object _lockRenderTarget = new object();

        private bool _resourcesInitialised;
        private Query _query;
        private Font _font;
        private bool _queryIssued;
        private ScreenshotRequest _requestCopy;
        private bool _renderTargetCopyLocked;
        private Surface _renderTargetCopy;
        private Surface _resolvedTarget;

        protected override string HookName => "DXHookD3D9";

        private List<IntPtr> _id3DDeviceFunctionAddresses = new List<IntPtr>();

        //List<IntPtr> id3dDeviceExFunctionAddresses = new List<IntPtr>();
        private const int D3D9DeviceMethodCount = 119;

        private const int D3D9ExDeviceMethodCount = 15;
        private bool _supportsDirect3D9Ex;

        public override void Hook()
        {
            DebugMessage("Hook: Begin");
            // First we need to determine the function address for IDirect3DDevice9
            _id3DDeviceFunctionAddresses = new List<IntPtr>();
            //id3dDeviceExFunctionAddresses = new List<IntPtr>();
            DebugMessage("Hook: Before device creation");
            using (var d3D = new Direct3D())
            {
                using (var renderForm = new Form())
                {
                    Device device;
                    using (device = new Device(d3D, 0, DeviceType.NullReference, IntPtr.Zero,
                        CreateFlags.HardwareVertexProcessing,
                        new PresentParameters
                        {
                            BackBufferWidth = 1,
                            BackBufferHeight = 1,
                            DeviceWindowHandle = renderForm.Handle
                        }))
                    {
                        DebugMessage("Hook: Device created");
                        _id3DDeviceFunctionAddresses.AddRange(GetVTblAddresses(device.NativePointer,
                            D3D9DeviceMethodCount));
                    }
                }
            }

            try
            {
                using (var d3DEx = new Direct3DEx())
                {
                    DebugMessage("Hook: Direct3DEx...");
                    using (var renderForm = new Form())
                    {
                        using (var deviceEx = new DeviceEx(d3DEx, 0, DeviceType.NullReference, IntPtr.Zero,
                            CreateFlags.HardwareVertexProcessing,
                            new PresentParameters
                            {
                                BackBufferWidth = 1,
                                BackBufferHeight = 1,
                                DeviceWindowHandle = renderForm.Handle
                            }, new DisplayModeEx {Width = 800, Height = 600}))
                        {
                            DebugMessage("Hook: DeviceEx created - PresentEx supported");
                            _id3DDeviceFunctionAddresses.AddRange(GetVTblAddresses(deviceEx.NativePointer,
                                D3D9DeviceMethodCount, D3D9ExDeviceMethodCount));
                            _supportsDirect3D9Ex = true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                _supportsDirect3D9Ex = false;
            }

            // We want to hook each method of the IDirect3DDevice9 interface that we are interested in

            // 42 - EndScene (we will retrieve the back buffer here)
            _direct3DDeviceEndSceneHook = new Hook<Direct3D9DeviceEndSceneDelegate>(
                _id3DDeviceFunctionAddresses[(int) Direct3DDevice9FunctionOrdinals.EndScene],
                // On Windows 7 64-bit w/ 32-bit app and d3d9 dll version 6.1.7600.16385, the address is equiv to:
                // (IntPtr)(GetModuleHandle("d3d9").ToInt32() + 0x1ce09),
                // A 64-bit app would use 0xff18
                // Note: GetD3D9DeviceFunctionAddress will output these addresses to a log file
                new Direct3D9DeviceEndSceneDelegate(EndSceneHook),
                this);

            unsafe
            {
                // If Direct3D9Ex is available - hook the PresentEx
                if (_supportsDirect3D9Ex)
                {
                    _direct3DDeviceExPresentExHook = new Hook<Direct3D9DeviceExPresentExDelegate>(
                        _id3DDeviceFunctionAddresses[(int) Direct3DDevice9ExFunctionOrdinals.PresentEx],
                        new Direct3D9DeviceExPresentExDelegate(PresentExHook),
                        this);
                }

                // Always hook Present also (device will only call Present or PresentEx not both)
                _direct3DDevicePresentHook = new Hook<Direct3D9DevicePresentDelegate>(
                    _id3DDeviceFunctionAddresses[(int) Direct3DDevice9FunctionOrdinals.Present],
                    new Direct3D9DevicePresentDelegate(PresentHook),
                    this);
            }

            // 16 - Reset (called on resolution change or windowed/fullscreen change - we will reset some things as well)
            _direct3DDeviceResetHook = new Hook<Direct3D9DeviceResetDelegate>(
                _id3DDeviceFunctionAddresses[(int) Direct3DDevice9FunctionOrdinals.Reset],
                // On Windows 7 64-bit w/ 32-bit app and d3d9 dll version 6.1.7600.16385, the address is equiv to:
                //(IntPtr)(GetModuleHandle("d3d9").ToInt32() + 0x58dda),
                // A 64-bit app would use 0x3b3a0
                // Note: GetD3D9DeviceFunctionAddress will output these addresses to a log file
                new Direct3D9DeviceResetDelegate(ResetHook),
                this);

            /*
             * Don't forget that all hooks will start deactivated...
             * The following ensures that all threads are intercepted:
             * Note: you must do this for each hook.
             */

            _direct3DDeviceEndSceneHook.Activate();
            Hooks.Add(_direct3DDeviceEndSceneHook);

            _direct3DDevicePresentHook.Activate();
            Hooks.Add(_direct3DDevicePresentHook);

            if (_supportsDirect3D9Ex)
            {
                _direct3DDeviceExPresentExHook.Activate();
                Hooks.Add(_direct3DDeviceExPresentExHook);
            }

            _direct3DDeviceResetHook.Activate();
            Hooks.Add(_direct3DDeviceResetHook);

            DebugMessage("Hook: End");
        }

        /// <summary>
        /// Just ensures that the surface we created is cleaned up.
        /// </summary>
        public override void Cleanup()
        {
            lock (_lockRenderTarget)
            {
                _resourcesInitialised = false;

                Utilities.Dispose(ref _renderTargetCopy);
                _renderTargetCopyLocked = false;

                Utilities.Dispose(ref _resolvedTarget);
                Utilities.Dispose(ref _query);
                _queryIssued = false;

                Utilities.Dispose(ref _font);

                Utilities.Dispose(ref _overlayEngine);
            }
        }

        /// <summary>
        /// The IDirect3DDevice9.EndScene function definition
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int Direct3D9DeviceEndSceneDelegate(IntPtr device);

        /// <summary>
        /// The IDirect3DDevice9.Reset function definition
        /// </summary>
        /// <param name="device"></param>
        /// <param name="presentParameters"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int Direct3D9DeviceResetDelegate(IntPtr device, ref PresentParameters presentParameters);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private unsafe delegate int Direct3D9DevicePresentDelegate(IntPtr devicePtr, Rectangle* pSourceRect,
            Rectangle* pDestRect, IntPtr hDestWindowOverride, IntPtr pDirtyRegion);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private unsafe delegate int Direct3D9DeviceExPresentExDelegate(IntPtr devicePtr, Rectangle* pSourceRect,
            Rectangle* pDestRect, IntPtr hDestWindowOverride, IntPtr pDirtyRegion, Present dwFlags);


        /// <summary>
        /// Reset the _renderTarget so that we are sure it will have the correct presentation parameters (required to support working across changes to windowed/fullscreen or resolution changes)
        /// </summary>
        /// <param name="devicePtr"></param>
        /// <param name="presentParameters"></param>
        /// <returns></returns>
        private int ResetHook(IntPtr devicePtr, ref PresentParameters presentParameters)
        {
            // Ensure certain overlay resources have performed necessary pre-reset tasks
            if (_overlayEngine != null)
                _overlayEngine.BeforeDeviceReset();

            Cleanup();

            return _direct3DDeviceResetHook.Original(devicePtr, ref presentParameters);
        }

        private bool _isUsingPresent;

        // Used in the overlay
        private unsafe int PresentExHook(IntPtr devicePtr, Rectangle* pSourceRect, Rectangle* pDestRect,
            IntPtr hDestWindowOverride, IntPtr pDirtyRegion, Present dwFlags)
        {
            _isUsingPresent = true;
            var device = (DeviceEx) devicePtr;

            DoCaptureRenderTarget(device, "PresentEx");

            return _direct3DDeviceExPresentExHook.Original(devicePtr, pSourceRect, pDestRect, hDestWindowOverride,
                pDirtyRegion, dwFlags);
        }

        private unsafe int PresentHook(IntPtr devicePtr, Rectangle* pSourceRect, Rectangle* pDestRect,
            IntPtr hDestWindowOverride, IntPtr pDirtyRegion)
        {
            _isUsingPresent = true;

            var device = (Device) devicePtr;

            DoCaptureRenderTarget(device, "PresentHook");

            return _direct3DDevicePresentHook.Original(devicePtr, pSourceRect, pDestRect, hDestWindowOverride,
                pDirtyRegion);
        }

        /// <summary>
        /// Hook for IDirect3DDevice9.EndScene
        /// </summary>
        /// <param name="devicePtr">Pointer to the IDirect3DDevice9 instance. Note: object member functions always pass "this" as the first parameter.</param>
        /// <returns>The HRESULT of the original EndScene</returns>
        /// <remarks>Remember that this is called many times a second by the Direct3D application - be mindful of memory and performance!</remarks>
        private int EndSceneHook(IntPtr devicePtr)
        {
            var device = (Device) devicePtr;

            if (!_isUsingPresent)
                DoCaptureRenderTarget(device, "EndSceneHook");

            return _direct3DDeviceEndSceneHook.Original(devicePtr);
        }

        private DxOverlayEngine _overlayEngine;

        /// <summary>
        /// Implementation of capturing from the render target of the Direct3D9 Device (or DeviceEx)
        /// </summary>
        /// <param name="device"></param>
        /// <param name="hook"></param>
        private void DoCaptureRenderTarget(Device device, string hook)
        {
            Frame();

            try
            {
                if (Config.ShowOverlay)
                {
                    #region Draw Overlay

                    // Check if overlay needs to be initialised
                    if (_overlayEngine == null || _overlayEngine.Device.NativePointer != device.NativePointer)
                    {
                        // Cleanup if necessary
                        if (_overlayEngine != null)
                            Utilities.Dispose(ref _overlayEngine);

                        _overlayEngine = ToDispose(new DxOverlayEngine());
                        var item = new TextElement(new System.Drawing.Font("Arial", 16, FontStyle.Bold))
                        {
                            Location = new Point(110, 5),
                            Color = Color.White,
                            AntiAliased = true
                        };
                        var item2 = new Memory(new System.Drawing.Font("Arial", 16, FontStyle.Bold))
                        {
                            Location = new Point(5, 20),
                            Text = "",
                            Color = Color.White,
                        };


                        // Create Overlay
                        _overlayEngine.Overlays.Add(new Common.Overlay
                        {
                            Elements =
                            {
                                item,
                                item2
                            }
                        });

                        _overlayEngine.Initialise(device);
                        Interface.PercentageText = item;
                        Interface.Message(MessageType.Information, "Overlay engine started");
                    }
                    // Draw Overlay(s)
                    else if (_overlayEngine != null)
                    {
                        foreach (var overlay in _overlayEngine.Overlays)
                            overlay.Frame();
                        _overlayEngine.Draw(Interface.ProgressPercentage);
                    }

                    #endregion
                }
            }
            catch (Exception e)
            {
                DebugMessage(e.ToString());
            }
        }

        private DataRectangle LockRenderTarget(Surface renderTargetCopy, out RawRectangle rect)
        {
            if (_requestCopy.RegionToCapture.Height > 0 && _requestCopy.RegionToCapture.Width > 0)
            {
                rect = new RawRectangle(_requestCopy.RegionToCapture.Left, _requestCopy.RegionToCapture.Top,
                    _requestCopy.RegionToCapture.Width, _requestCopy.RegionToCapture.Height);
            }
            else
            {
                rect = new RawRectangle(0, 0, renderTargetCopy.Description.Width, renderTargetCopy.Description.Height);
            }
            return renderTargetCopy.LockRectangle(rect, LockFlags.ReadOnly);
        }

        private void CreateResources(Device device, int width, int height, Format format)
        {
            if (_resourcesInitialised) return;
            _resourcesInitialised = true;

            // Create offscreen surface to use as copy of render target data
            _renderTargetCopy =
                ToDispose(Surface.CreateOffscreenPlain(device, width, height, format, Pool.SystemMemory));

            // Create our resolved surface (resizing if necessary and to resolve any multi-sampling)
            _resolvedTarget =
                ToDispose(Surface.CreateRenderTarget(device, width, height, format, MultisampleType.None, 0, false));

            _query = ToDispose(new Query(device, QueryType.Event));
        }
    }
}