using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Features.SyntaxHighlighting;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Host.Features.SyntaxHighlighting
{
    [Language(typeof (ShaderLabLanguage))]
    internal class ShaderLabSyntaxHighlightingManager : RiderSyntaxHighlightingManager
    {
        public override RiderSyntaxHighlightingProcessBase CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            IFile file)
        {
            return new ShaderLabSyntaxHighlightingProcess(process, settings, file);
        }
    }
}