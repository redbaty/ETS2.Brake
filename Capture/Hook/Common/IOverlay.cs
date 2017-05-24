using System.Collections.Generic;

namespace Overlay.Hook.Common
{
    public interface IOverlay: IOverlayElement
    {
        List<IOverlayElement> Elements { get; set; }
    }
}
