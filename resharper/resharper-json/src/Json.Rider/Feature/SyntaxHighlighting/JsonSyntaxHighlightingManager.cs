using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.Syntax;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Rider.Feature.SyntaxHighlighting
{
    // Override RiderSyntaxHighlightingManager (registered for all known languages) which provides simple syntax
    // highlighting based on token node types (string literal, keyword, comment, number, etc.). We don't want any
    // syntax highlighting, as it's handled by the frontend
    [Language(typeof(JsonNewLanguage))]
    internal class JsonSyntaxHighlightingManager : SyntaxHighlightingManager
    {
        public override SyntaxHighlightingStageProcess CreateProcess(IDaemonProcess process,
                                                                     IContextBoundSettingsStore settings,
                                                                     IFile getPrimaryPsiFile)
        {
            return null;
        }
    }
}
