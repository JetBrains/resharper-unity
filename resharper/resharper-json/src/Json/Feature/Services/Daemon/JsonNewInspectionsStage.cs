using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Feature.Services.Daemon
{
    [DaemonStage]
    public class JsonInspectionsStage : JsonNewDaemonStageBase
    {
        private readonly ElementProblemAnalyzerRegistrar myElementProblemAnalyzerRegistrar;

        public JsonInspectionsStage(ElementProblemAnalyzerRegistrar elementProblemAnalyzerRegistrar)
        {
            myElementProblemAnalyzerRegistrar = elementProblemAnalyzerRegistrar;
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process,
                                                             IContextBoundSettingsStore settings,
                                                             DaemonProcessKind processKind, IJsonNewFile file)
        {
            return new JsonNewInspectionsProcess(process, settings, file, processKind,
                myElementProblemAnalyzerRegistrar);
        }

        private class JsonNewInspectionsProcess : JsonNewDaemonStageProcessBase
        {
            private readonly IElementAnalyzerDispatcher myElementAnalyzerDispatcher;

            public JsonNewInspectionsProcess(IDaemonProcess process, IContextBoundSettingsStore settingsStore,
                                             IJsonNewFile file, DaemonProcessKind processKind,
                                             ElementProblemAnalyzerRegistrar elementProblemAnalyzerRegistrar)
                : base(process, file)
            {
                var problemAnalyzerData = new ElementProblemAnalyzerData(
                    file, settingsStore, ElementProblemAnalyzerRunKind.FullDaemon, process.GetCheckForInterrupt());

                problemAnalyzerData.SetDaemonProcess(process, processKind);

                myElementAnalyzerDispatcher = elementProblemAnalyzerRegistrar.CreateDispatcher(problemAnalyzerData);
            }

            public override void VisitNode(ITreeNode node, IHighlightingConsumer context)
            {
                myElementAnalyzerDispatcher.Run(node, context);
                base.VisitNode(node, context);
            }
        }
    }
}