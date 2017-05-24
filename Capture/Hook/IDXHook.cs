using System;
using Overlay.Interface;

namespace Overlay.Hook
{
    public interface IDXHook: IDisposable
    {
        OverlayInterface Interface
        {
            get;
            set;
        }
        OverlayConfig Config
        {
            get;
            set;
        }

        ScreenshotRequest Request
        {
            get;
            set;
        }

        void Hook();

        void Cleanup();
    }
}
