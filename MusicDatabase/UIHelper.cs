using System;
using System.Linq;
using System.Windows.Media;
using MusicDatabase.Engine;

namespace MusicDatabase
{
    public static class UIHelper
    {
        private static byte Blend(byte a, byte b, double coef)
        {
            return (byte)(a * coef + b * (1 - coef));
        }

        public static Color Blend(Color a, Color b, double coef)
        {
            return Color.FromArgb(
                Blend(a.A, b.A, coef),
                Blend(a.R, b.R, coef),
                Blend(a.G, b.G, coef),
                Blend(a.B, b.B, coef)
                );
        }

        internal static SolidColorBrush GetDrBrush(int dr, double alpha = 1)
        {
            double coef = Utility.Clamp((dr - 7) / 7.0, 0, 1);
            byte alphaByte = (byte)(alpha * 255);
            Color drGreen = Color.FromArgb(alphaByte, 0, 0xC4, 0);
            Color drRed = Color.FromArgb(alphaByte, 0xC4, 0, 0);
            return new SolidColorBrush(UIHelper.Blend(drGreen, drRed, coef));
        }
    }
}
