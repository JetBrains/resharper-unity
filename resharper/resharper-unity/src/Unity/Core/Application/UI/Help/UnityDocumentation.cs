#nullable enable
using System;
using System.Text;
using JetBrains.Application;
using JetBrains.Application.I18n;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Help
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityDocumentation
    {
        private readonly CultureContextComponent myCultureContextComponent;
        private readonly ILogger myLogger;

        public UnityDocumentation(CultureContextComponent cultureContextComponent, ILogger logger)
        {
            myCultureContextComponent = cultureContextComponent;
            myLogger = logger;
        }

        /// <summary>Returns either file path uri (for one of <paramref name="offlineKeywords"/>) or web uri for <paramref name="onlineKeyword"/> if matching file doc wasn't found.</summary>
        public Uri GetDocumentationUri(IUnityVersion? unityVersion, UnityDocumentationCatalog catalog, HybridCollection<string> offlineKeywords, string onlineKeyword)
        {
            var langCode = LangCodeMap.GetUnityLangCode(myCultureContextComponent.Culture.Value, myLogger);
            if (unityVersion == null)
                return catalog.GetSearchUri(GetBaseSearchUri(null, langCode), onlineKeyword);
            
            var documentationRoot = GetDocumentationRoot(unityVersion, langCode);
            foreach (var keyword in offlineKeywords)
            {
                if (catalog.TryGetFile(documentationRoot, keyword) is { } filePath)
                    return filePath.ToUri();
            }

            return catalog.GetSearchUri(GetBaseSearchUri(unityVersion.ActualVersionForSolution.Value, langCode), onlineKeyword);
        }

        private FileSystemPath GetDocumentationRoot(IUnityVersion unityVersion, string langCode)
        {
            var appPath = unityVersion.GetActualAppPathForSolution();
            if (appPath.IsEmpty) return FileSystemPath.Empty;

            var contentsPath = UnityInstallationFinder.GetApplicationContentsPath(appPath);
            var root = contentsPath.Combine("Documentation");

            // ~/Unity/Hub/Editor/2021.2.4f1/Editor/Data/Documentation/Documentation/en path on my machine. Most likely Linux only peculiarity
            var potentialRoot = root.Combine("Documentation");
            if (potentialRoot is { IsAbsolute: true, ExistsDirectory: true })
                root = potentialRoot;
            
            // /Applications/Unity/Hub/Editor/6000.0.61f1/Documentation
            // /Applications/Unity/Hub/Editor/6000.4.0b2/Documentation.
            var potentialRoot2 = appPath.Parent.Combine("Documentation");
            if (potentialRoot2 is { IsAbsolute: true, ExistsDirectory: true })
                root = potentialRoot2;

            if (root.IsEmpty || !root.ExistsDirectory)
                return FileSystemPath.Empty;

            // first choice would be current lang
            if (!string.IsNullOrEmpty(langCode))
            {
                var langRoot = root.Combine(langCode);
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

        private static Uri GetBaseSearchUri(Version? version, string unityLangCode)
        {
            var sb = new StringBuilder("https://docs.unity3d.com");

            if (unityLangCode == "en")
                unityLangCode = string.Empty;

            if (!string.IsNullOrEmpty(unityLangCode))
                sb.Append($"/{unityLangCode}");
            
            if (version == null || version.Major == 0)
            {
                // when version is unknown, there is a difference between urls for lang code
                // https://docs.unity3d.com/cn/current/ScriptReference/30_search.html?q=MonoBehaviour.OnTriggerStay2D
                // https://docs.unity3d.com/ScriptReference/30_search.html?q=MonoBehaviour.OnTriggerStay2D
                if (!string.IsNullOrEmpty(unityLangCode)) sb.Append("/current");
                return new Uri(sb.ToString());
            }

            // Version before 2017.1 has different format of version:
            // https://docs.unity3d.com/560/Documentation/ScriptReference/MonoBehaviour.html
            if (version < new Version(2017, 1))
                sb.Append($"/{version.Major}{version.Minor}0");
            else
                sb.Append($"/{version.ToString(2)}");

            // en url has additional Documentation part
            // https://docs.unity3d.com/kr/2021.1/ScriptReference/30_search.html?q=MonoBehaviour.OnCollisionEnter
            // https://docs.unity3d.com/2021.1/Documentation/ScriptReference/30_search.html?q=MonoBehaviour.OnCollisionEnter
            if (string.IsNullOrEmpty(unityLangCode))
                sb.Append("/Documentation");
            return new Uri(sb.ToString());
        }
    }
}
