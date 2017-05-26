using System;

namespace Overlay.Hook
{
    public class TextDisplay
    {
        readonly long _startTickCount = 0;

        public TextDisplay()
        {
            _startTickCount = DateTime.Now.Ticks;
            Display = true;
        }

        /// <summary>
        /// Must be called each frame
        /// </summary>
        public void Frame()
        {
            if (Display && Math.Abs(DateTime.Now.Ticks - _startTickCount) > Duration.Ticks)
            {
                Display = false;
            }
        }

        public bool Display { get; private set; }
        public TimeSpan Duration { private get; set; }
        public float Remaining
        {
            get
            {
                if (Display)
                {
                    return (float)Math.Abs(DateTime.Now.Ticks - _startTickCount) / (float)Duration.Ticks;
                }
                else
                {
                    return 0;
                }
            }
        }

        public string Text { get; set; }
    }
}
