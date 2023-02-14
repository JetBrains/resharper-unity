using System;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.I18n;
using JetBrains.Application.StdApplicationUI;
using JetBrains.Application.UI.Help;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Help
{
    [ShellComponent]
    public class ShowUnityHelp : IShowHelp
    {
        private readonly OpensUri myUriOpener;
        private readonly SolutionsManager mySolutionsManager;
        private readonly ILogger myLogger;
        private readonly CultureContextComponent myCultureContextComponent;

        public ShowUnityHelp(OpensUri uriOpener, SolutionsManager solutionsManager, ILogger logger, CultureContextComponent cultureContextComponent)
        {
            myUriOpener = uriOpener;
            mySolutionsManager = solutionsManager;
            myLogger = logger;
            myCultureContextComponent = cultureContextComponent;
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

            if (IsUnityKeyword(keyword))
                return StripPrefix(keyword);
            return keyword;
        }

        [NotNull]
        public Uri GetUri([NotNull] string keyword)
        {
            // VS specifies keyword as SerializeField.#ctor
            // Rider specifies keyword as SerializeField.-ctor
            // result for offline doc should be SerializeField-ctor/SerializeField, depending on presence of specific doc on the disk
            // result for online doc should contain dot, otherwise nothing is found

            var unityLangCode = LangCodeMap.GetUnityLangCode(myCultureContextComponent.Culture.Value, myLogger);
            var documentationRoot = GetDocumentationRoot(unityLangCode);
            var offlineKeyword = keyword.Replace(".#", "-").Replace(".-", "-");
            var res = GetFileUri(documentationRoot, $"ScriptReference/{offlineKeyword}.html") // ctor or type
                      ?? GetFileUri(documentationRoot, $"ScriptReference/{offlineKeyword.ReplaceLast('.', '-')}.html") // property
                      ?? GetFileUri(documentationRoot, $"ScriptReference/{offlineKeyword.Replace("-ctor", "")}.html") // ctor in Rider doesn't exist, so goto type doc
                      ?? new Uri($"https://docs.unity3d.com{GetVersionLanguageSpecificPieceOfUrl(unityLangCode)}/ScriptReference/30_search.html?q={keyword.Replace(".#", ".").Replace(".-", ".")}"); // fallback to online doc
            
            myLogger.Trace($"GetUri offlineKeyword:{offlineKeyword}, onlineKeyword:{keyword.Replace(".#", ".").Replace(".-", ".")} {res}");
            return res;
        }
        
        private string GetVersionLanguageSpecificPieceOfUrl(string unityLangCode)
        {
            var sb = new StringBuilder();

            if (unityLangCode == "en")
                unityLangCode = string.Empty;
            
            if (!string.IsNullOrEmpty(unityLangCode))
                sb.Append($"/{unityLangCode}");

            var version = mySolutionsManager.Solution?.GetComponent<UnityVersion>().ActualVersionForSolution.Value;
            if (version == null || version.Major == 0)
            {
                // when version is unknown, there is a difference between urls for lang code
                // https://docs.unity3d.com/cn/current/ScriptReference/30_search.html?q=MonoBehaviour.OnTriggerStay2D
                // https://docs.unity3d.com/ScriptReference/30_search.html?q=MonoBehaviour.OnTriggerStay2D
                if (!string.IsNullOrEmpty(unityLangCode)) sb.Append("/current");
                return sb.ToString();
            }
            
            // Version before 2017.1 has different format of version:
            // https://docs.unity3d.com/560/Documentation/ScriptReference/MonoBehaviour.html
            //var result = string.Empty;
            if (version < new Version(2017, 1))
                sb.Append($"/{version.Major}{version.Minor}0");
            else
                sb.Append($"/{version.ToString(2)}");

            // en url has additional Documentation part
            // https://docs.unity3d.com/kr/2021.1/ScriptReference/30_search.html?q=MonoBehaviour.OnCollisionEnter
            // https://docs.unity3d.com/2021.1/Documentation/ScriptReference/30_search.html?q=MonoBehaviour.OnCollisionEnter
            if (string.IsNullOrEmpty(unityLangCode))
                sb.Append("/Documentation");
            return sb.ToString();
        }

        [NotNull]
        private FileSystemPath GetDocumentationRoot(string unityLangCode)
        {
            var appPath = mySolutionsManager.Solution?.GetComponent<UnityVersion>().GetActualAppPathForSolution();
            if (appPath == null || appPath.IsEmpty) return FileSystemPath.Empty;
            var contentsPath = UnityInstallationFinder.GetApplicationContentsPath(appPath);
            var root = contentsPath.Combine("Documentation");

            // I see /home/ivan-shakhov/Unity/Hub/Editor/2021.2.4f1/Editor/Data/Documentation/Documentation/en path on my machine
            // most likely Linux only peculiarity
            var potentialRoot = root.Combine("Documentation");
            if (potentialRoot.IsAbsolute && potentialRoot.ExistsDirectory)
                root = potentialRoot;
            
            if (root.IsEmpty || !root.ExistsDirectory)
                return FileSystemPath.Empty;

            // first choice would be current lang
            if (!string.IsNullOrEmpty(unityLangCode))
            {
                var langRoot = root.Combine(unityLangCode);
                if (langRoot.ExistsDirectory)
                    return langRoot.ToNativeFileSystemPath();
            }
            
            // second choice - english
            var englishRoot = root.Combine("en");
            if (englishRoot.ExistsDirectory)
                return englishRoot.ToNativeFileSystemPath();    
            
            // third choice - anything in the folder
            return root.GetChildDirectories().FirstOrDefault(englishRoot).ToNativeFileSystemPath();
        }

        [CanBeNull]
        private static Uri GetFileUri([NotNull] FileSystemPath documentationRoot, string htmlPath)
        {
            if (documentationRoot.IsEmpty)
                return null;

            var fileSystemPath = documentationRoot/htmlPath;
            return fileSystemPath.IsAbsolute && fileSystemPath.ExistsFile ? fileSystemPath.ToUri() : null;
        }
    }
}