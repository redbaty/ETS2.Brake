using System.Collections.Generic;

namespace Overlay.Hook.Common
{
    public sealed class Overlay: IOverlay
    {
        public List<IOverlayElement> Elements { get; set; } = new List<IOverlayElement>();

        public bool Hidden
        {
            get;
            set;
        }

        public void Frame()
        {
            foreach (var element in Elements)
            {
                element.Frame();
            }
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
