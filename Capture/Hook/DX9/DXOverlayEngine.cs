using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using Overlay.Elements;
using Overlay.Extensions;
using Overlay.Hook.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;

namespace Overlay.Hook.DX9
{
    [Serializable]
    public class DxOverlayEngine : Component
    {
        public List<IOverlay> Overlays { get; }

        private bool _initialised;
        private bool _initialising;

        private Sprite _sprite;
        private Sprite _progressBarSprite;
        private readonly Dictionary<string, Font> _fontCache = new Dictionary<string, Font>();
        private readonly Dictionary<Element, Texture> _imageCache = new Dictionary<Element, Texture>();

        private Image BackgroundImage { get; }
        private Image ForegroundImage { get; }

        public Device Device { get; private set; }

        public DxOverlayEngine()
        {
            Overlays = new List<IOverlay>();
            ForegroundImage = ToDispose(GetImage(Brushes.CornflowerBlue));
            BackgroundImage = ToDispose(GetImage(Brushes.White));
        }

        private void EnsureInitiliased()
        {
            Debug.Assert(_initialised);
        }

        public bool Initialise(Device device)
        {
            Debug.Assert(!_initialised);
            if (_initialising)
                return false;

            _initialising = true;

            try
            {
                Device = device;

                _sprite = ToDispose(new Sprite(Device));
                _progressBarSprite = ToDispose(new Sprite(Device));

                // Initialise any resources required for overlay elements
                IntialiseElementResources();

                _initialised = true;
                return true;
            }
            finally
            {
                _initialising = false;
            }
        }

        private void IntialiseElementResources()
        {
            foreach (var overlay in Overlays)
            {
                foreach (var element in overlay.Elements)
                {
                    var textElement = element as TextElement;
                    var imageElement = element as ImageElement;

                    if (textElement != null)
                    {
                        GetFontForTextElement(textElement);
                    }
                    else if (imageElement != null)
                    {
                        GetImageForImageElement(imageElement);
                    }
                }
            }
        }

        private void Begin()
        {
            _sprite.Begin(SpriteFlags.AlphaBlend);
        }

        /// <summary>
        /// Draw the overlay(s)
        /// </summary>
        public void Draw(int progressSize)
        {
            EnsureInitiliased();

            DrawTextElements();
            DrawProgresBar(progressSize);
        }

        private void DrawTextElements()
        {
            Begin();

            foreach (var overlay in Overlays)
            {
                foreach (var element in overlay.Elements)
                {
                    if (element.Hidden)
                        continue;

                    var textElement = element as TextElement;
                    var imageElement = element as ImageElement;

                    if (textElement != null)
                    {
                        var font = GetFontForTextElement(textElement);

                        if (font != null && !string.IsNullOrEmpty(textElement.Text))
                            font.DrawText(_sprite, textElement.Text, textElement.Location.X, textElement.Location.Y,
                                new ColorBGRA(textElement.Color.R, textElement.Color.G, textElement.Color.B,
                                    textElement.Color.A));
                    }
                    else if (imageElement != null)
                    {
                        var image = GetImageForImageElement(imageElement);
                        if (image != null)
                            _sprite.Draw(image,
                                new ColorBGRA(imageElement.Tint.R, imageElement.Tint.G, imageElement.Tint.B,
                                    imageElement.Tint.A), null, null,
                                new Vector3(imageElement.Location.X, imageElement.Location.Y, 0));
                    }
                }
            }

            End();
        }

        private void DrawProgresBar(int progressSize)
        {
            _progressBarSprite.Begin(SpriteFlags.AlphaBlend);

            var backgroundstream = BackgroundImage.ToStream(ImageFormat.Bmp);
            var foregroundstream = ForegroundImage.ToStream(ImageFormat.Bmp);
            var backgroundTexture = Texture.FromStream(Device, backgroundstream, 100, 16, 0,
                Usage.None,
                Format.A8B8G8R8, Pool.Default, Filter.Default, Filter.Default, 0);

            var foregroundTexture = Texture.FromStream(Device, foregroundstream,
                progressSize, 16, 0,
                Usage.None,
                Format.A8B8G8R8, Pool.Default, Filter.Default, Filter.Default, 0);

            var color = new ColorBGRA(0xffffffff);
            var pos = new Vector3(5, 5, 0);

            _progressBarSprite.Draw(backgroundTexture, color, null, null, pos);

            if (progressSize > 0)
                _progressBarSprite.Draw(foregroundTexture, color, null, null, pos);

            _progressBarSprite.End();
            backgroundstream.Dispose();
            foregroundstream.Dispose();
            backgroundTexture.Dispose();
            foregroundTexture.Dispose();
        }

        private static Image GetImage(Brush color)
        {
            Image resultImage = new Bitmap(1440, 90, PixelFormat.Format24bppRgb);

            using (var grp = Graphics.FromImage(resultImage))
            {
                grp.FillRectangle(
                    color, 0, 0, resultImage.Width, resultImage.Height);
            }

            return resultImage;
        }

        private void End()
        {
            _sprite.End();
        }

        /// <summary>
        /// In Direct3D9 it is necessary to call OnLostDevice before any call to device.Reset(...) for certain interfaces found in D3DX (e.g. ID3DXSprite, ID3DXFont, ID3DXLine) - https://msdn.microsoft.com/en-us/library/windows/desktop/bb172979(v=vs.85).aspx
        /// </summary>
        public void BeforeDeviceReset()
        {
            try
            {
                foreach (var item in _fontCache)
                    item.Value.OnLostDevice();

                _sprite?.OnLostDevice();
            }
            catch
            {
            }
        }

        private Font GetFontForTextElement(TextElement element)
        {
            var fontKey = $"{element.Font.Name}{element.Font.Size}{element.Font.Style}";

            if (!_fontCache.TryGetValue(fontKey, out Font result))
            {
                result = ToDispose(new Font(Device, new FontDescription
                {
                    FaceName = element.Font.Name,
                    Italic = (element.Font.Style & FontStyle.Italic) == FontStyle.Italic,
                    Quality = (element.AntiAliased ? FontQuality.Antialiased : FontQuality.Default),
                    Weight = ((element.Font.Style & FontStyle.Bold) == FontStyle.Bold)
                        ? FontWeight.Bold
                        : FontWeight.Normal,
                    Height = (int) element.Font.SizeInPoints
                }));
                _fontCache[fontKey] = result;
            }
            return result;
        }

        private Texture GetImageForImageElement(ImageElement element)
        {
            Texture result = null;

            if (!string.IsNullOrEmpty(element.Filename))
            {
                if (!_imageCache.TryGetValue(element, out result))
                {
                    result = ToDispose(Texture.FromFile(Device, element.Filename));

                    _imageCache[element] = result;
                }
            }
            return result;
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources
        /// </summary>
        /// <param name="disposing">true if disposing both unmanaged and managed</param>
        protected override void Dispose(bool disposing)
        {
            if (true)
            {
                Device = null;
            }
        }

        private void SafeDispose(IDisposable disposableObj)
        {
            disposableObj?.Dispose();
        }
    }
}