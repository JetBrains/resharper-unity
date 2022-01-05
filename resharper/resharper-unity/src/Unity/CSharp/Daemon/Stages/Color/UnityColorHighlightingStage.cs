using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Color
{
    [DaemonStage(StagesBefore = new[] {typeof(IdentifierHighlightingStage)})]
    public class UnityColorHighlightingStage : CSharpDaemonStageBase
    {
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            if (processKind == DaemonProcessKind.VISIBLE_DOCUMENT &&
                settings.GetValue(HighlightingSettingsAccessor.ColorUsageHighlightingEnabled))
            {
                return new UnityColorHighlighterProcess(process, settings, file);
            }
            return null;
        }

        protected override bool IsSupported(IPsiSourceFile sourceFile)
        {
            if (sourceFile == null || !sourceFile.IsValid())
                return false;

            return sourceFile.IsLanguageSupported<CSharpLanguage>();
        }
    }
}