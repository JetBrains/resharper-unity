using System.Collections.Generic;
using System.Drawing;
using JetBrains.ReSharper.Psi.Colors;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Colors
{
    public static class UnityNamedColors
    {
        // TODO: Would this be easier as a Dictionary<string, Color>?
        private static readonly Dictionary<string, uint> NamedColors =
            new Dictionary<string, uint>()
            {
                {"red", 0xFFFF0000},
                {"green", 0xFF00FF00},
                {"blue", 0xFF0000FF},
                {"white", 0xFFFFFFFF},
                {"black", 0xFF000000},
                {"yellow", 0xFFFFEB04},
                {"cyan", 0xFF00FFFF},
                {"magenta", 0xFFFF00FF},
                {"gray", 0xFF7F7F7F},
                {"grey", 0xFF7F7F7F},
                {"clear", 0x00000000}
            };

        public static Color? Get(string name)
        {
            uint value;
            if (name != null && NamedColors.TryGetValue(name, out value))
                return ToColor(value);
            return null;
        }

        public static IEnumerable<IColorElement> GetColorTable()
        {
            foreach (var namedColor in NamedColors)
            {
                yield return new ColorElement(ToColor(namedColor.Value), namedColor.Key);
            }
        }

        private static Color ToColor(uint color)
        {
            var value = color;

            return Color.FromArgb(
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value);
        }

        public static string GetColorName(Color color)
        {
            foreach (var namedColor in NamedColors)
            {
                if (ToColor(namedColor.Value) == color)
                    return namedColor.Key;
            }

            return null;
        }
    }
}