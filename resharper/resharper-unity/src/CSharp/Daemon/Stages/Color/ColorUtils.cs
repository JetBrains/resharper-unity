using System;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Color
{
    // See http://stackoverflow.com/a/1626175/88374
    // And http://www.splinter.com.au/converting-hsv-to-rgb-colour-using-c/
    internal static class ColorUtils
    {
        // Normalised so that h is 0..1, not 0..360
        public static void ColorToHSV(System.Drawing.Color color, out float hue, out float saturation, out float value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue() / 360.0f;
            saturation = (float) (max == 0 ? 0 : 1d - 1d * min / max);
            value = (float) (max / 255d);
        }

        // Expects h as 0..1, not 0..360
        public static System.Drawing.Color ColorFromHSV(float hue, float saturation, float value)
        {
            if (value <= 0)
                return System.Drawing.Color.FromArgb(0, 0, 0);
            if (saturation <= 0)
                return FromRgb(value, value, value);

            double hf = hue * 6.0;
            int i = (int) Math.Floor(hf) % 6;
            double f = hf - i;

            float v = value;
            double pv = value * (1.0 - saturation);
            double qv = value * (1.0 - saturation * f);
            double tv = value * (1.0 - saturation * (1.0 - f));

            switch (i)
            {
                // Red is the dominant colour
                case 0: return FromRgb(v, tv, pv);

                // Green is the dominant colour
                case 1: return FromRgb(qv, v, pv);
                case 2: return FromRgb(pv, v, tv);

                // Blue is the dominant colour
                case 3: return FromRgb(pv, qv, v);
                case 4: return FromRgb(tv, pv, v);

                // Red is the dominant colour
                default: return FromRgb(v, pv, qv);
            }
        }

        private static System.Drawing.Color FromRgb(double r, double g, double b)
        {
            return System.Drawing.Color.FromArgb((int) (r * 255.0), (int) (g * 255.0), (int) (b * 255.0));
        }
    }
}