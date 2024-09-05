using JetBrains.Application.Environment;
using JetBrains.Application.Environment.Helpers;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages
{
    // TODO: Implement file structure and usages!
    [DaemonStage(Instantiation.DemandAnyThreadSafe, StagesBefore = new[] { typeof(GlobalFileStructureCollectorStage) },
        StagesAfter = new [] { typeof(CollectUsagesStage)} )]
    public class IdentifierHighlightingStage : ShaderLabStageBase
    {
        private readonly ResolveHighlighterRegistrar myRegistrar;
        private readonly bool myInternalMode;

        public IdentifierHighlightingStage(ResolveHighlighterRegistrar registrar,
            RunsProducts.ProductConfigurations productConfigurations)
        {
            myRegistrar = registrar;
            myInternalMode = productConfigurations.IsInternalMode();
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, IShaderLabFile file)
        {
            return new IdentifierHighlighterProcess(process, myRegistrar, settings, processKind, file, myInternalMode);
        }

        protected override bool IsSupported(IPsiSourceFile sourceFile)
        {
            // Don't check PSI properties
            if (sourceFile == null || !sourceFile.IsValid())
                return false;

            return sourceFile.IsLanguageSupported<ShaderLabLanguage>();
        }
    }
}