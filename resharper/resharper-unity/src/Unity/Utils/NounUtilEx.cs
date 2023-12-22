using System;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    public class NounUtilEx
    {
        [NotNull]
        public static string ToPluralOrSingularQuick(int count, bool estimatedResult, [NotNull] string singular, [NotNull] string plural)
        {
            if (singular == null) throw new ArgumentNullException(nameof(singular));
            if (plural == null) throw new ArgumentNullException(nameof(plural));
      
            return count == 1 && !estimatedResult ? singular : plural;
        }

        [NotNull]
        public static string ToEmptyPluralOrSingularQuick(int count, bool estimatedResult, [NotNull] string noText,
            [NotNull] string singular, [NotNull] string plural)
        {
            if (count == 0 && !estimatedResult)
                return noText;

            var countTextWithPlus = count + (estimatedResult ? "+" : "");
            return
                $"{countTextWithPlus} {ToPluralOrSingularQuick(count, estimatedResult, singular, plural)}";
        }
    }
}