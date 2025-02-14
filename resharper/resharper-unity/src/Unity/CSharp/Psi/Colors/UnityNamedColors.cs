using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.Util.Media;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Colors
{
    public static class UnityNamedColors
    {
        /* Dump colors in debugger with:
         string.Join(",", typeof(Color).GetProperties().Where(a=>MemberUtility.IsStatic(a)).ToArray().Select(a=>(a.Name, ((Color)a.GetValue(null)).ToHexString())).ToArray().Select(a=>$"{{\"{a.Item1}\", 0x{a.Item2}}}"))
         */
        // I have tried using JetRgbaColors, but 21 colors out of 157 didn't have matching counterparts.

        private static readonly Dictionary<string, uint> NamedColors =
            new Dictionary<string, uint>()
            {
                { "aliceBlue", 0xF0F8FFFF }, { "antiqueWhite", 0xFAEBD7FF }, { "aquamarine", 0x7EFFD4FF },
                { "azure", 0xF0FFFFFF }, { "beige", 0xF5F5DCFF }, { "bisque", 0xFFE4C4FF }, { "black", 0x000000FF },
                { "blanchedAlmond", 0xFFEBCDFF }, { "blue", 0x0000FFFF }, { "blueViolet", 0x8A2BE2FF },
                { "brown", 0xA42A2AFF }, { "burlywood", 0xDEB787FF }, { "cadetBlue", 0x5E9EA0FF },
                { "chartreuse", 0x7EFF00FF }, { "chocolate", 0xD2691EFF }, { "clear", 0x00000000 },
                { "coral", 0xFF7E50FF }, { "cornflowerBlue", 0x6495EDFF }, { "cornsilk", 0xFFF8DCFF },
                { "crimson", 0xDC143BFF }, { "cyan", 0x00FFFFFF }, { "darkBlue", 0x00008BFF },
                { "darkCyan", 0x008B8BFF }, { "darkGoldenRod", 0xB7860BFF }, { "darkGray", 0xA9A9A9FF },
                { "darkGreen", 0x006400FF }, { "darkKhaki", 0xBDB76BFF }, { "darkMagenta", 0x8B008BFF },
                { "darkOliveGreen", 0x546B2EFF }, { "darkOrange", 0xFF8B00FF }, { "darkOrchid", 0x9931CCFF },
                { "darkRed", 0x8B0000FF }, { "darkSalmon", 0xE9967AFF }, { "darkSeaGreen", 0x8EBC8EFF },
                { "darkSlateBlue", 0x483D8BFF }, { "darkSlateGray", 0x2E4E4EFF }, { "darkTurquoise", 0x00CED1FF },
                { "darkViolet", 0x9400D3FF }, { "deepPink", 0xFF1493FF }, { "deepSkyBlue", 0x00BFFFFF },
                { "dimGray", 0x696969FF }, { "dodgerBlue", 0x1E90FFFF }, { "firebrick", 0xB12121FF },
                { "floralWhite", 0xFFFAF0FF }, { "forestGreen", 0x218B21FF }, { "gainsboro", 0xDCDCDCFF },
                { "ghostWhite", 0xF8F8FFFF }, { "gold", 0xFFD700FF }, { "goldenRod", 0xDAA420FF },
                { "gray", 0x7F7F7FFF }, { "grey", 0x7F7F7FFF }, { "gray1", 0x191919FF }, { "gray2", 0x333333FF },
                { "gray3", 0x4C4C4CFF }, { "gray4", 0x666666FF }, { "gray5", 0x7F7F7FFF }, { "gray6", 0x999999FF },
                { "gray7", 0xB2B2B2FF }, { "gray8", 0xCCCCCCFF }, { "gray9", 0xE5E5E5FF }, { "green", 0x00FF00FF },
                { "greenYellow", 0xADFF2EFF }, { "honeydew", 0xF0FFF0FF }, { "hotPink", 0xFF69B4FF },
                { "indianRed", 0xCD5B5BFF }, { "indigo", 0x4B0082FF }, { "ivory", 0xFFFFF0FF }, { "khaki", 0xF0E68BFF },
                { "lavender", 0xE6E6FAFF }, { "lavenderBlush", 0xFFF0F5FF }, { "lawnGreen", 0x7CFC00FF },
                { "lemonChiffon", 0xFFFACDFF }, { "lightBlue", 0xADD8E6FF }, { "lightCoral", 0xF08080FF },
                { "lightCyan", 0xE0FFFFFF }, { "lightGoldenRod", 0xEEDD82FF }, { "lightGoldenRodYellow", 0xFAFAD2FF },
                { "lightGray", 0xD3D3D3FF }, { "lightGreen", 0x90EE90FF }, { "lightPink", 0xFFB6C1FF },
                { "lightSalmon", 0xFFA07AFF }, { "lightSeaGreen", 0x20B1AAFF }, { "lightSkyBlue", 0x87CEFAFF },
                { "lightSlateBlue", 0x8470FFFF }, { "lightSlateGray", 0x778899FF }, { "lightSteelBlue", 0xB0C4DEFF },
                { "lightYellow", 0xFFFFE0FF }, { "limeGreen", 0x31CD31FF }, { "linen", 0xFAF0E6FF },
                { "magenta", 0xFF00FFFF }, { "maroon", 0xB03060FF }, { "mediumAquamarine", 0x66CDAAFF },
                { "mediumBlue", 0x0000CDFF }, { "mediumOrchid", 0xBA54D3FF }, { "mediumPurple", 0x9370DBFF },
                { "mediumSeaGreen", 0x3BB371FF }, { "mediumSlateBlue", 0x7B68EEFF },
                { "mediumSpringGreen", 0x00FA9AFF }, { "mediumTurquoise", 0x48D1CCFF },
                { "mediumVioletRed", 0xC71585FF }, { "midnightBlue", 0x191970FF }, { "mintCream", 0xF5FFFAFF },
                { "mistyRose", 0xFFE4E1FF }, { "moccasin", 0xFFE4B4FF }, { "navajoWhite", 0xFFDEADFF },
                { "navyBlue", 0x000080FF }, { "oldLace", 0xFDF5E6FF }, { "olive", 0x808000FF },
                { "oliveDrab", 0x6B8E22FF }, { "orange", 0xFFA400FF }, { "orangeRed", 0xFF4400FF },
                { "orchid", 0xDA70D6FF }, { "paleGoldenRod", 0xEEE8AAFF }, { "paleGreen", 0x98FB98FF },
                { "paleTurquoise", 0xAFEEEEFF }, { "paleVioletRed", 0xDB7093FF }, { "papayaWhip", 0xFFEFD5FF },
                { "peachPuff", 0xFFDAB9FF }, { "peru", 0xCD853EFF }, { "pink", 0xFFC0CBFF }, { "plum", 0xDDA0DDFF },
                { "powderBlue", 0xB0E0E6FF }, { "purple", 0xA020F0FF }, { "rebeccaPurple", 0x663399FF },
                { "red", 0xFF0000FF }, { "rosyBrown", 0xBC8E8EFF }, { "royalBlue", 0x4169E1FF },
                { "saddleBrown", 0x8B4413FF }, { "salmon", 0xFA8072FF }, { "sandyBrown", 0xF4A460FF },
                { "seaGreen", 0x2E8B57FF }, { "seashell", 0xFFF5EEFF }, { "sienna", 0xA0512DFF },
                { "silver", 0xC0C0C0FF }, { "skyBlue", 0x87CEEBFF }, { "slateBlue", 0x6A5ACDFF },
                { "slateGray", 0x708090FF }, { "snow", 0xFFFAFAFF }, { "softRed", 0xDC3131FF },
                { "softBlue", 0x30AEBFFF }, { "softGreen", 0x8BC924FF }, { "softYellow", 0xFFEE8BFF },
                { "springGreen", 0x00FF7EFF }, { "steelBlue", 0x4582B4FF }, { "tan", 0xD2B48BFF },
                { "teal", 0x008080FF }, { "thistle", 0xD8BFD8FF }, { "tomato", 0xFF6347FF },
                { "turquoise", 0x40E0D0FF }, { "violet", 0xEE82EEFF }, { "violetRed", 0xD02090FF },
                { "wheat", 0xF5DEB3FF }, { "white", 0xFFFFFFFF }, { "whiteSmoke", 0xF5F5F5FF },
                { "yellow", 0xFFEB04FF }, { "yellowGreen", 0x9ACD31FF }, { "yellowNice", 0xFFEB04FF }
            };

        public static JetRgbaColor? Get(string name)
        {
            if (name != null && NamedColors.TryGetValue(name, out var value))
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

        private static JetRgbaColor ToColor(uint color)
        {
            var value = color;

            return JetRgbaColor.FromRgba(
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value);
        }

        public static string GetColorName(JetRgbaColor color)
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