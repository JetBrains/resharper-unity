using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.Explanatory;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.Explanatory
{
    [ShellComponent]
    public class UnityCodeInspectionWikiDataProvider : ICodeInspectionWikiDataProvider
    {
        private static readonly IDictionary<string, string> ourUrls =
            new Dictionary<string, string>
            {
                // TODO: Temporary URLs
                {
                    UnityObjectNullCoalescingWarning.HIGHLIGHTING_ID,
                    "https://github.com/JetBrains/resharper-unity/blob/feature/why_is_rider_suggesting_this/docs/wiki/UnityObjectNullComparisons.md"
                },
                {
                    UnityObjectNullPropagationWarning.HIGHLIGHTING_ID,
                    "https://github.com/JetBrains/resharper-unity/blob/feature/why_is_rider_suggesting_this/docs/wiki/UnityObjectNullComparisons.md"
                }
            };

        public bool TryGetValue(string attributeId, out string url)
        {
            return ourUrls.TryGetValue(attributeId, out url);
        }
    }
}