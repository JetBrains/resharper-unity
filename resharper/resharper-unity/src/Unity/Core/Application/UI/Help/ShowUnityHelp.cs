using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.StdApplicationUI;
using JetBrains.Application.UI.Help;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.OnlineHelp;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Help
{
    [ShellComponent]
    public class ShowUnityHelp : IShowHelp
    {
        private readonly OpensUri myUriOpener;
        private readonly SolutionsManager mySolutionsManager;
        private readonly ILogger myLogger;
        private readonly UnityDocumentation myUnityDocumentation;
        private static readonly Regex ourGenericTypeSuffixRegex = new Regex(@"`\d+");

        public ShowUnityHelp(OpensUri uriOpener, SolutionsManager solutionsManager, ILogger logger, UnityDocumentation unityDocumentation)
        {
            myUriOpener = uriOpener;
            mySolutionsManager = solutionsManager;
            myLogger = logger;
            myUnityDocumentation = unityDocumentation;
        }

        public bool ShowHelp(string keyword, HelpSystem.HelpKind kind, string preferredProduct = "")
        {
            if (kind != HelpSystem.HelpKind.Msdn) return false;
            if (!IsUnityKeyword(keyword)) return false;
            keyword = StripPrefix(keyword);

            var uri = GetUri(keyword);
            myUriOpener.OpenUri(uri);

            return true;
        }

        // Must be less than `VsShowMsdnHelp` so we get a shot at handling it
        public double Priority => 0;
        public HelpSystem.HelpKind[] SupportedKinds => new[] {HelpSystem.HelpKind.Msdn};

        private static bool IsUnityKeyword(string keyword)
        {
            return keyword.StartsWith("UnityEngine.", StringComparison.OrdinalIgnoreCase)
                   || keyword.StartsWith("UnityEditor.", StringComparison.OrdinalIgnoreCase);
        }

        private static string StripPrefix(string keyword)
        {
            // We know the string starts with `UnityEngine.` or `UnityEditor.`
            return keyword.Substring(12);
        }

        public static string FormatDocumentationKeyword(string keyword)
        {
            if (keyword == null)
                return null;

            var formatDocumentationKeyword = IsUnityKeyword(keyword) ? StripPrefix(keyword) : keyword;
            return StripGenericSuffix(formatDocumentationKeyword);
        }

        private static string StripGenericSuffix(string formatDocumentationKeyword)
        {
            return ourGenericTypeSuffixRegex.Replace(formatDocumentationKeyword, string.Empty);
        }

        [NotNull]
        public Uri GetUri([NotNull] string keyword)
        {
            // VS specifies keyword as SerializeField.#ctor
            // Rider specifies keyword as SerializeField.-ctor
            // result for offline doc should be SerializeField-ctor/SerializeField, depending on presence of specific doc on the disk
            // result for online doc should contain dot, otherwise nothing is found
            var offlineKeyword = keyword.Replace(".#", "-").Replace(".-", "-");
            var offlineKeywords = new HybridCollection<string>(
                offlineKeyword, // ctor or type
                offlineKeyword.ReplaceLast('.', '-'), // property
                offlineKeyword.Replace("-ctor", "")  // ctor in Rider doesn't exist, so goto type doc
            );
            var onlineKeyword = keyword.Replace(".#", ".").Replace(".-", ".");
            var res = myUnityDocumentation.GetDocumentationUri(mySolutionsManager.Solution?.GetComponent<IUnityVersion>(), UnityDocumentationCatalog.ScriptReference, offlineKeywords, onlineKeyword);
            myLogger.Trace($"GetUri offlineKeyword:{offlineKeyword}, onlineKeyword:{onlineKeyword} {res}");
            return res;
        }

        public void ShowOnlineHelp<TProvider>(IDeclaredElement element) where TProvider : class, IOnlineHelpProvider
        {
            var url = element.GetSolution().GetComponent<TProvider>().GetUrl(element);
            if (url != null)
                myUriOpener.OpenUri(url);
        }
    }
}