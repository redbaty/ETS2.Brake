using System;
using System.Drawing;
using Overlay.Interface;

namespace Overlay.Elements
{
    internal class Memory : TextElement
    {
        public Memory(Font font) : base(font)
        {
            Init();
        }

        public Memory(Font font, OverlayInterface Interface) : base(font)
        {
            Init();
            this.Interface = Interface;
        }

        private void Init()
        {
            Text = "";
        }

        private OverlayInterface Interface { get; }

        public override bool Hidden => Interface.ShowMemoryUsage;

        public override string Text => $"Memory usage: {Convert.ToInt32(GC.GetTotalMemory(true) / 1024f)}MB";
    }
}