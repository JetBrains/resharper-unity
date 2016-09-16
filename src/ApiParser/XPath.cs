using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace ApiParser
{
    public static class XPath
    {
        private static readonly Regex ClassRegex = new Regex( @"(^|/)(\w+)(\[.*\])?((\.\w+)+)" );
        private static readonly Regex IdRegex = new Regex( @"(^|/)(\w+)#(\w+)" );

        [NotNull]
        public static string Resolve([NotNull] string xpath)
        {
            xpath = IdRegex.Replace(xpath, "$1$2[@id='$3']");
            xpath = ClassRegex.Replace(xpath, TranslateClass);
            return xpath.Replace("][", " and ");
        }

        [NotNull]
        private static string TranslateClass([NotNull]Match m)
        {
            return $"{m.Groups[1]}{m.Groups[2]}{m.Groups[3]}[@class='{m.Groups[4].Value.Replace('.', ' ').Trim()}']";
        }
    }
}