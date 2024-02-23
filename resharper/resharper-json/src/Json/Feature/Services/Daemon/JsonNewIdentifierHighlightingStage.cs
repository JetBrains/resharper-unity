using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Feature.Services.Daemon
{
    [DaemonStage(StagesBefore = [typeof(GlobalFileStructureCollectorStage)])]
    public class JsonNewIdentifierHighlightingStage : JsonNewDaemonStageBase
    {
        private readonly ResolveHighlighterRegistrar myRegistrar;

        public JsonNewIdentifierHighlightingStage(ResolveHighlighterRegistrar registrar)
        {
            myRegistrar = registrar;
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
                                                             DaemonProcessKind processKind, IJsonNewFile file)
        {
            return new JsonNewIdentifierHighlightingProcess(process, myRegistrar, file);
        }

        private class JsonNewIdentifierHighlightingProcess : JsonNewDaemonStageProcessBase
        {
            private readonly ResolveProblemHighlighter myResolveProblemHighlighter;
            private readonly IReferenceProvider myReferenceProvider;

            public JsonNewIdentifierHighlightingProcess([NotNull] IDaemonProcess process,
                                                        ResolveHighlighterRegistrar registrar,
                                                        IJsonNewFile file)
                : base(process, file)
            {
                myResolveProblemHighlighter = new ResolveProblemHighlighter(registrar);
                myReferenceProvider = ((IFileImpl)file).ReferenceProvider;
            }

            public override void VisitNode(ITreeNode node, IHighlightingConsumer consumer)
            {
                var references = node.GetReferences(myReferenceProvider);
                myResolveProblemHighlighter.CheckForResolveProblems(references, consumer);
            }
        }
    }
}