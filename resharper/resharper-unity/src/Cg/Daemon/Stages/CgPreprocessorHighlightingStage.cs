using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Stages
{
    [DaemonStage(StagesBefore = new[] { typeof(GlobalFileStructureCollectorStage) },
        StagesAfter = new [] { typeof(CollectUsagesStage)} )]
    public class CgPreprocessorHighlightingStage : CgDaemonStageBase
    {
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICgFile file)
        {
            return new CgPreprocessorHighlightingProcess(process, file);
        }

        private class CgPreprocessorHighlightingProcess : CgDaemonStageProcessBase
        {
            public CgPreprocessorHighlightingProcess(IDaemonProcess daemonProcess, ICgFile file)
                : base(daemonProcess, file)
            {
            }

            public override void VisitDirectiveNode(IDirective directiveParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.KEYWORD, directiveParam.HeaderNode.GetDocumentRange()));
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.PREPROCESSOR_LINE_CONTENT, directiveParam.ContentNode.GetDocumentRange()));

                base.VisitDirectiveNode(directiveParam, context);
            }
        }
    }
}