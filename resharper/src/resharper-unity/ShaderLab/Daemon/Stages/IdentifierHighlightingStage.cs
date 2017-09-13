using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages
{
    // TODO: Implement file structure and usages!
    [DaemonStage(StagesBefore = new[] { typeof(GlobalFileStructureCollectorStage) },
        StagesAfter = new [] { typeof(CollectUsagesStage)} )]
    public class IdentifierHighlightingStage : ShaderLabStageBase
    {
        private readonly ResolveHighlighterRegistrar myRegistrar;
        private readonly ConfigurableIdentifierHighlightingStageService myIdentifierHighlightingStageService;

        public IdentifierHighlightingStage(ResolveHighlighterRegistrar registrar, ConfigurableIdentifierHighlightingStageService identifierHighlightingStageService)
        {
            myRegistrar = registrar;
            myIdentifierHighlightingStageService = identifierHighlightingStageService;
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, IShaderLabFile file)
        {
            return new IdentifierHighlighterProcess(process, myRegistrar, settings, processKind, file, myIdentifierHighlightingStageService);
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