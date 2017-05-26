using System;
using System.Drawing;

namespace Overlay.Elements
{
    internal class Memory : TextElement
    {
        public Memory(Font font) : base(font)
        {
        }

        public override string Text => $"Memory usage: {Convert.ToInt32(GC.GetTotalMemory(true) / (1024f))}MB";
    }
}