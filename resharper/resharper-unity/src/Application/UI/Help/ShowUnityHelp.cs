using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.StdApplicationUI;
using JetBrains.Application.UI.Help;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Application.UI.Help
{
    [ShellComponent]
    public class ShowUnityHelp : IShowHelp
    {
        private readonly OpensUri myUriOpener;
        private readonly SolutionsManager mySolutionsManager;

        public ShowUnityHelp(OpensUri uriOpener, SolutionsManager solutionsManager)
        {
            myUriOpener = uriOpener;
            mySolutionsManager = solutionsManager;
        }

        public bool ShowHelp(string keyword, HelpSystem.HelpKind kind, string preferredProduct = "")
        {
            return ShowHelp(keyword, kind);
        }

        public bool ShowHelp(string keyword, HelpSystem.HelpKind kind)
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

        [NotNull]
        private Uri GetUri(string keyword)
        {
            var documentationRoot = GetDocumentationRoot();
            return GetFileUri(documentationRoot, $"ScriptReference/{keyword}.html")
                   ?? GetFileUri(documentationRoot, $"ScriptReference/{keyword.Replace('.', '-')}.html")
                   ?? new Uri($"https://docs.unity3d.com/ScriptReference/30_search.html?q={keyword}");
        }

        [NotNull]
        private FileSystemPath GetDocumentationRoot()
        {
            var appPath = mySolutionsManager.Solution?.GetComponent<UnityVersion>().GetActualAppPathForSolution();
            var contentsPath = UnityInstallationFinder.GetApplicationContentsPath(appPath);
            return contentsPath.Combine(@"Documentation/en");
        }

        [CanBeNull]
        private static Uri GetFileUri([NotNull] FileSystemPath documentationRoot, string htmlPath)
        {
            if (documentationRoot.IsEmpty)
                return null;

            var fileSystemPath = documentationRoot/htmlPath;
            return fileSystemPath.ExistsFile ? fileSystemPath.ToUri() : null;
        }
    }
}