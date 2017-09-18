using JetBrains.Application.Settings;
using JetBrains.ReSharper.Cg.Daemon.Errors;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Stages
{
    [DaemonStage(StagesBefore = new[] { typeof(GlobalFileStructureCollectorStage) },
                 StagesAfter = new [] { typeof(CollectUsagesStage)} )]
    public class CgSyntaxHighlightingStage : CgDaemonStageBase
    {
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICgFile file)
        {
            return new CgSyntaxHighlightingProcess(process, settings, file);
        }

        private class CgSyntaxHighlightingProcess : CgDaemonStageProcessBase
        {
            public CgSyntaxHighlightingProcess(IDaemonProcess daemonProcess, IContextBoundSettingsStore settingsStore, ICgFile file)
                : base(daemonProcess, settingsStore, file)
            {
            }
            
            public override void VisitConstantValueNode(IConstantValue constantValueParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.NUMBER, constantValueParam.GetDocumentRange()));
                base.VisitConstantValueNode(constantValueParam, context);
            }
#if DEBUG            
            public override void VisitNode(ITreeNode node, IHighlightingConsumer context)
            {
                if (node is IErrorElement errorElement)
                {
                    var range = errorElement.GetDocumentRange();
                    if (!range.IsValid())
                        range = node.Parent.GetDocumentRange();
                    if (range.TextRange.IsEmpty)
                    {
                        if (range.TextRange.EndOffset < range.Document.GetTextLength())
                            range = range.ExtendRight(1);
                        else
                            range = range.ExtendLeft(1);
                    }
                    context.AddHighlighting(new CgSyntaxError(errorElement.ErrorDescription, range), range);
                }

                base.VisitNode(node, context);
            }
#endif                    
        }
    }
}