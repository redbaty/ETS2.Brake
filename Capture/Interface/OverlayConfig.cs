using System;

namespace Overlay.Interface
{
    [Serializable]
    public class OverlayConfig
    {
        public Direct3DVersion Direct3DVersion { get; set; }
        public bool ShowOverlay { get; set; }

        public OverlayConfig()
        {
            Direct3DVersion = Direct3DVersion.Direct3D9;
            ShowOverlay = false;
        }
    }
}
