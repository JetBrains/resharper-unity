#if RIDER

using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Host.Features.Foldings.ShaderLab
{
    [DaemonStage(StagesBefore = new[] { typeof(GlobalFileStructureCollectorStage) },
        StagesAfter = new [] { typeof(CollectUsagesStage)} )]
    public class CodeFoldingHighlightingStage : ShaderLabStageBase
    {
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, IShaderLabFile file)
        {
            return new CodeFoldingProcess(process, settings, file);
        }
    }
}

#endif