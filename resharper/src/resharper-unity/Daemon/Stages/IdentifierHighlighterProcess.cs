using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.VisualElements;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages
{
    internal class IdentifierHighlighterProcess : ShaderLabDaemonStageProcessBase
    {
        private readonly DaemonProcessKind myProcessKind;
        private readonly bool myIdentifierHighlightingEnabled;
        private readonly VisualElementHighlighter myVisualElementHighlighter;

        public IdentifierHighlighterProcess(IDaemonProcess process, ResolveHighlighterRegistrar registrar,
            IContextBoundSettingsStore settingsStore, DaemonProcessKind processKind, IShaderLabFile file,
            ConfigurableIdentifierHighlightingStageService identifierHighlightingStageService)
            : base(process, settingsStore, file)
        {
            myProcessKind = processKind;
            myIdentifierHighlightingEnabled = identifierHighlightingStageService.ShouldHighlightIdentifiers(settingsStore);
            myVisualElementHighlighter = new VisualElementHighlighter(ShaderLabLanguage.Instance, settingsStore);
        }

        public override void VisitNode(ITreeNode node, IHighlightingConsumer context)
        {
            if (myProcessKind == DaemonProcessKind.VISIBLE_DOCUMENT)
            {
                if (myIdentifierHighlightingEnabled)
                {
                    var info = myVisualElementHighlighter.CreateColorHighlightingInfo(node);
                    if (info != null)
                        context.AddHighlighting(info);
                }
            }

            // TODO: Resolve problem highlighter

            var errorNode = node as IErrorElement;
            if (errorNode != null)
            {
                context.AddHighlighting(new ShaderLabSyntaxError(errorNode.ErrorDescription, node.GetDocumentRange()));
            }

            base.VisitNode(node, context);
        }
    }
}