using System;
using System.Drawing;

namespace Overlay.Elements
{
    class Memory : TextElement
    {
        private string _text;

        public Memory(Font font) : base(font)
        {
        }

        public override string Text
        {
            get => _text;
            set
            {
                _text = value;
                _text = $"Memory usage: {GC.GetTotalMemory(true) / 1024}MB";;
            }
        }
    }
}