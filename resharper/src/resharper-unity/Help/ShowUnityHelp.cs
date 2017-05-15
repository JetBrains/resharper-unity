using System;
using JetBrains.Application;
using JetBrains.Application.StdApplicationUI;
using JetBrains.Util;

#if WAVE08
using JetBrains.UI.Application;
#else
using JetBrains.Application.UI.Help;
#endif

namespace JetBrains.ReSharper.Plugins.Unity.Help
{
    [ShellComponent]
    public class ShowUnityHelp : IShowHelp
    {
        private readonly OpensUri myUriOpener;

        public ShowUnityHelp(OpensUri uriOpener)
        {
            myUriOpener = uriOpener;
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

        private static Uri GetUri(string keyword)
        {
            var path = $"ScriptReference/{keyword}.html";

            return GetUri(GetDocumentationRoot(), path)
                   ?? new Uri("https://docs.unity3d.com/" + path);
        }

        private static FileSystemPath GetDocumentationRoot()
        {
            switch (PlatformUtil.RuntimePlatform)
            {
                case PlatformUtil.Platform.Windows:
                    var programFiles = GetProgramFiles();
                    if (!programFiles.IsEmpty)
                        return GetProgramFiles().Combine("Unity/Editor/Data/Documentation");
                    return FileSystemPath.Empty;

                case PlatformUtil.Platform.MacOsX:
                    return FileSystemPath.Parse("/Applications/Unity/Documentation");

                case PlatformUtil.Platform.Linux:
                    // TODO: I don't know if this value is correct...
                    return FileSystemPath.Parse("/opt/Unity/Editor/Data/Documentation");
            }
            return FileSystemPath.Empty;
        }

        private static FileSystemPath GetProgramFiles()
        {
            // PlatformUtils.GetProgramFiles() will return the relevant folder for
            // the current app, not the current system. So a 32 bit app on a 64 bit
            // system will return the 32 bit Program Files. Force to get the system
            // native Program Files folder
            var environmentVariable = Environment.GetEnvironmentVariable("ProgramW6432");
            return string.IsNullOrWhiteSpace(environmentVariable) ? FileSystemPath.Empty : FileSystemPath.TryParse(environmentVariable);
        }

        private static Uri GetUri(FileSystemPath documentationRoot, string htmlPath)
        {
            if (documentationRoot.IsEmpty)
                return null;

            var fileSystemPath = documentationRoot/"en"/htmlPath;
            return fileSystemPath.ExistsFile ? fileSystemPath.ToUri() : null;
        }
    }
}