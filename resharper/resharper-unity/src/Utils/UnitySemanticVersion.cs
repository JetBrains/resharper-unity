using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    // Wrappers to make it look like Unity's product version is actually semver compatible. It simply converts
    // e.g. 2019.4.12f1 to 2019.4.12-f1, but never lets that be visible
    public class UnitySemanticVersion
    {
        private readonly string myOriginalExpression;

        private UnitySemanticVersion(JetSemanticVersion semanticVersion, string originalExpression)
        {
            myOriginalExpression = originalExpression;
            SemanticVersion = semanticVersion;
        }

        public JetSemanticVersion SemanticVersion { get; }

        public override string ToString() => myOriginalExpression;

        public static bool TryParse(string originalVersion, out UnitySemanticVersion result)
        {
            result = null;

            if (JetSemanticVersion.TryParse(originalVersion, out var semanticVersion))
            {
                result = new UnitySemanticVersion(semanticVersion, originalVersion);
                return true;
            }

            return false;
        }

        public static bool TryParseProductVersion(string originalVersion, out UnitySemanticVersion result)
        {
            result = null;

            var compatibleVersion = Regex.Replace(originalVersion, @"(\d+\.\d+\.\d+)([abf]\d+)", "$1-$2");
            if (JetSemanticVersion.TryParse(compatibleVersion, out var semanticVersion))
            {
                result = new UnitySemanticVersion(semanticVersion, originalVersion);
                return true;
            }

            return false;
        }
    }

    public class UnitySemanticVersionRange
    {
        private readonly JetSemanticVersionRange myRange;
        private readonly string myCompatibleExpression;

        private UnitySemanticVersionRange(JetSemanticVersionRange range)
        {
            myRange = range;
            myCompatibleExpression = Regex.Replace(range.ToString(), @"(\d+\.\d+\.\d+)-([abf]\d+)", "$1$2");
        }

        public bool IsValid(UnitySemanticVersion version)
        {
            return myRange.IsValid(version.SemanticVersion);
        }

        public override string ToString() => myCompatibleExpression;

        public static bool TryParse(string expression, out UnitySemanticVersionRange result)
        {
            result = null;

            // The expression might include Unity's nearly-but-not-quite semver compatible product version. Let's fudge
            // it so that it is compatible. This works for comparisons, but use the wrappers to keep the original
            // expressions around for display strings
            var compatibleExpression = Regex.Replace(expression, @"(\d+\.\d+\.\d+)([abf]\d+)", "$1-$2");
            if (JetSemanticVersionRange.TryParse(compatibleExpression, out var range))
            {
                result = new UnitySemanticVersionRange(range);
                return true;
            }

            return true;
        }

        public static UnitySemanticVersionRange Parse(string expression)
        {
            if (TryParse(expression, out var range))
                return range;
            throw new InvalidDataException($"Cannot parse expression: {expression}");
        }
    }
}