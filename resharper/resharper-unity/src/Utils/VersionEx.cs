using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    public static class VersionEx
    {
        // Like Version.CompareTo but does not compare undefined components. E.g. 2020.1 == 2020.1.2
        // Useful for API comparisons. If the API has a min version of 5.0 and max of 2020.1, then it should also be
        // valid for 2020.1.2
        [SuppressMessage("ReSharper", "ArrangeRedundantParentheses")]
        public static int CompareToLenient([NotNull] this Version self, [NotNull] Version other)
        {
            return ReferenceEquals(other, self) ? 0 :
                self.Major != other.Major ? (self.Major > other.Major ? 1 : -1) :
                self.Minor == -1 || other.Minor == -1 ? 0 :
                self.Minor != other.Minor ? (self.Minor > other.Minor ? 1 : -1) :
                self.Build == -1 || other.Build == -1 ? 0 :
                self.Build != other.Build ? (self.Build > other.Build ? 1 : -1) :
                self.Revision == -1 || other.Revision == -1 ? 0 :
                self.Revision != other.Revision ? (self.Revision > other.Revision ? 1 : -1) :
                0;
        }
    }
}