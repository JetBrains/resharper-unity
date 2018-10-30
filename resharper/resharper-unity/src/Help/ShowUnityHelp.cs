using System;
using JetBrains.Application;
using JetBrains.Application.StdApplicationUI;
using JetBrains.Application.UI.Help;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Help
{
    [ShellComponent]
    public class ShowUnityHelp : IShowHelp
    {
        private readonly OpensUri myUriOpener;
        private readonly UnityInstallationFinder myInstallationFinder;
        private readonly SolutionsManager mySolutionsManager;

        public ShowUnityHelp(OpensUri uriOpener, UnityInstallationFinder installationFinder, SolutionsManager solutionsManager)
        {
            myUriOpener = uriOpener;
            myInstallationFinder = installationFinder;
            mySolutionsManager = solutionsManager;
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

        private Uri GetUri(string keyword)
        {
            var documentationRoot = GetDocumentationRoot();
            return GetFileUri(documentationRoot, $"ScriptReference/{keyword}.html")
                   ?? GetFileUri(documentationRoot, $"ScriptReference/{keyword.Replace('.', '-')}.html")
                   ?? new Uri($"https://docs.unity3d.com/ScriptReference/30_search.html?q={keyword}");
        }
        
        private FileSystemPath GetDocumentationRoot()
        {
            var version = mySolutionsManager.Solution?.GetComponent<UnityVersion>().GetActualVersionForSolution();
            var contentsPath = myInstallationFinder.GetApplicationContentsPath(version);
            return contentsPath == null ? FileSystemPath.Empty : contentsPath.Combine(@"Documentation/en");
        
        }

        private static Uri GetFileUri(FileSystemPath documentationRoot, string htmlPath)
        {
            if (documentationRoot.IsEmpty)
                return null;

            var fileSystemPath = documentationRoot/htmlPath;
            return fileSystemPath.ExistsFile ? fileSystemPath.ToUri() : null;
        }
    }
}