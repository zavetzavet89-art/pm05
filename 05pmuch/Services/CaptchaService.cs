using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography;

namespace _05pmuch.Services
{
    public static class CaptchaService
    {
        private static readonly char[] _chars =
            "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789".ToCharArray();

        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        public static string GenerateText(int length)
        {
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));

            var result = new char[length];
            var buffer = new byte[4];

            for (int i = 0; i < length; i++)
            {
                _rng.GetBytes(buffer);
                int value = BitConverter.ToInt32(buffer, 0) & 0x7FFFFFFF;
                result[i] = _chars[value % _chars.Length];
            }

            return new string(result);
        }

        public static Bitmap GenerateImage(string text, int width, int height)
        {
            if (string.IsNullOrEmpty(text)) throw new ArgumentNullException(nameof(text));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                using (var brush = new LinearGradientBrush(new Rectangle(0, 0, width, height),
                    Color.FromArgb(245, 230, 210), Color.FromArgb(220, 195, 160), 60f))
                {
                    g.FillRectangle(brush, 0, 0, width, height);
                }

                var rnd = new Random(GetSeed());

                int lines = Math.Max(4, text.Length + 1);
                for (int i = 0; i < lines; i++)
                {
                    var penColor = Color.FromArgb(rnd.Next(80, 180), rnd.Next(80, 140), rnd.Next(40, 100), rnd.Next(20, 80));
                    using (var pen = new Pen(penColor, rnd.Next(1, 2)))
                    {
                        g.DrawLine(pen, rnd.Next(width), rnd.Next(height), rnd.Next(width), rnd.Next(height));
                    }
                }

                float charArea = width / (float)text.Length;
                float fontSize = height * 0.62f;
                var fontFamilies = new[] { "Georgia", "Tahoma", "Times New Roman", "Arial" };

                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    float x = i * charArea + (charArea - fontSize * 0.6f) / 2f;
                    float y = (height - fontSize) / 2f + rnd.Next(-6, 7);
                    float angle = rnd.Next(-28, 29);

                    using (var f = new Font(fontFamilies[rnd.Next(fontFamilies.Length)], fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                    {
                        var state = g.Save();
                        var cx = x + charArea / 2f;
                        var cy = y + fontSize / 2f;
                        g.TranslateTransform(cx, cy);
                        g.RotateTransform(angle);

                        var textColor = Color.FromArgb(rnd.Next(40, 160), rnd.Next(60, 120), rnd.Next(30, 90), rnd.Next(20, 70));
                        using (var textBrush = new SolidBrush(textColor))
                        {
                            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            g.DrawString(c.ToString(), f, textBrush, 0, 0, sf);
                        }

                        g.Restore(state);
                    }
                }

                int dots = (width * height) / 80;
                for (int i = 0; i < dots; i++)
                {
                    bmp.SetPixel(rnd.Next(width), rnd.Next(height),
                        Color.FromArgb(rnd.Next(80, 200), rnd.Next(100, 200), rnd.Next(60, 140), rnd.Next(30, 100)));
                }
            }

            return bmp;
        }

        private static int GetSeed()
        {
            var buffer = new byte[4];
            _rng.GetBytes(buffer);
            return BitConverter.ToInt32(buffer, 0) & 0x7FFFFFFF;
        }
    }
}
