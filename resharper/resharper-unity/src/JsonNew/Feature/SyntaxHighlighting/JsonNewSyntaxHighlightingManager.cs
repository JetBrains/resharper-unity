using JetBrains.Application.Settings;
using JetBrains.RdBackend.Common.Features.SyntaxHighlighting;
using JetBrains.ReSharper.Daemon.SyntaxHighlighting;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Feature.SyntaxHighlighting
{
    // Syntax highlighting provided by frontend
    [Language(typeof(JsonNewLanguage))]
    internal class JsonNewSyntaxHighlightingManager : RiderSyntaxHighlightingManager
    {
        public override SyntaxHighlightingStageProcess CreateProcess(IDaemonProcess process,
            IContextBoundSettingsStore settings,
            IFile getPrimaryPsiFile)
        {
            return null;
        }
    }
}