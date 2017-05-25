using System;

namespace Overlay.Elements
{
    [Serializable]
    public class FramesPerSecond: TextElement
    {
        public static string FpsFormat = "{0:N0} fps";

        public override string Text
        {
            get => string.Format(FpsFormat, GetFPS());
            set => FpsFormat = value;
        }

        int _frames = 0;
        int _lastTickCount = 0;
        float _lastFrameRate = 0;

        public FramesPerSecond(System.Drawing.Font font)
            : base(font)
        {
        }

        /// <summary>
        /// Must be called each frame
        /// </summary>
        public override void Frame()
        {
            _frames++;
            if (Math.Abs(Environment.TickCount - _lastTickCount) > 1000)
            {
                _lastFrameRate = (float)_frames * 1000 / Math.Abs(Environment.TickCount - _lastTickCount);
                _lastTickCount = Environment.TickCount;
                _frames = 0;
            }
        }

        /// <summary>
        /// Return the current frames per second
        /// </summary>
        /// <returns></returns>
        public float GetFPS()
        {
            return _lastFrameRate;
        }
    }
}
