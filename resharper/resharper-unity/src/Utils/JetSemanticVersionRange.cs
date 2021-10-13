using System.Diagnostics.CodeAnalysis;
using System.IO;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    public class JetSemanticVersionRange
    {
        private readonly JetSemanticVersion myFrom;
        private readonly JetSemanticVersion? myTo;
        private readonly bool myFromInclusive;
        private readonly bool myToInclusive;

        private JetSemanticVersionRange(JetSemanticVersion from, JetSemanticVersion? to,
                                        bool fromInclusive, bool toInclusive)
        {
            myFrom = from;
            myTo = to;
            myFromInclusive = to == null || fromInclusive;
            myToInclusive = to == null || toInclusive;
        }

        public bool IsValid(JetSemanticVersion version)
        {
            if (myFromInclusive)
            {
                if (version < myFrom)
                    return false;
            }
            else
            {
                if (version <= myFrom)
                    return false;
            }

            if (myTo != null)
            {
                if (myToInclusive)
                {
                    if (version > myTo)
                        return false;
                }
                else
                {
                    if (version >= myTo)
                        return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            if (myFrom == myTo)
                return $"x = {myFrom}";
            if (myTo == null)
                return $"x >= {myFrom}";
            var fromComparison = myFromInclusive ? "<=" : "<";
            var toComparison = myToInclusive ? "<=" : "<";
            return $"{myFrom} {fromComparison} x {toComparison} {myTo}";
        }

        public static JetSemanticVersionRange Parse(string expression)
        {
            if (!TryParse(expression, out var range))
                throw new InvalidDataException($"Cannot parse version range: {expression}");
            return range;
        }

        public static bool TryParse(string expression, [NotNullWhen(true)] out JetSemanticVersionRange? range)
        {
            range = null;

            if (expression.Contains(" ")) return false;

            var fromInclusive = expression.StartsWith("[");
            var fromExclusive = expression.StartsWith("(");
            var toInclusive = expression.EndsWith("]");
            var toExclusive = expression.EndsWith(")");

            // If we have an opening bracket, we need a closing bracket
            if ((fromExclusive | fromInclusive) != (toExclusive | toInclusive))
                return false;

            var startIdx = expression.StartsWith("[") || expression.StartsWith("(") ? 1 : 0;
            var endIdx = expression.EndsWith("]") || expression.EndsWith(")") ? 1 : 0;
            expression = expression.Substring(startIdx, expression.Length - startIdx - endIdx);

            var versions = expression.Split(',');
            if (JetSemanticVersion.TryParse(versions[0], out var fromVersion))
            {
                // 1.2.3 => x >= 1.2.3
                // [1.2.3] => x = 1.2.3
                if (versions.Length == 1)
                {
                    if (fromInclusive && toInclusive)
                        range = new JetSemanticVersionRange(fromVersion, fromVersion, fromInclusive, toInclusive);
                    else
                        range = new JetSemanticVersionRange(fromVersion, null, fromInclusive, toInclusive);
                    return true;
                }

                if (JetSemanticVersion.TryParse(versions[1], out var toVersion))
                {
                    range = new JetSemanticVersionRange(fromVersion, toVersion, fromInclusive, toInclusive);
                    return true;
                }
            }

            return false;
        }
    }
}