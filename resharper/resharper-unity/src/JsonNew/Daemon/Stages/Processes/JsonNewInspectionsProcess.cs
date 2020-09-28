using System;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.JavaScript.Stages.TypeScript.Error.ProblemAnalyzers;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Daemon.Stages.Processes
{
    public class JsonNewInspectionsProcess : JsonNewDaemonStageProcessBase
    {
        private readonly IElementAnalyzerDispatcher myElementAnalyzerDispatcher;

        public JsonNewInspectionsProcess(IDaemonProcess process, IContextBoundSettingsStore settingsStore, IJsonNewFile file, DaemonProcessKind processKind, ElementProblemAnalyzerRegistrar elementProblemAnalyzerRegistrar)
            : base(process, settingsStore, file)
        {
            var problemAnalyzerData = new ElementProblemAnalyzerData(
                file, settingsStore, ElementProblemAnalyzerRunKind.FullDaemon, process.GetCheckForInterrupt());

            problemAnalyzerData.SetDaemonProcess(process, processKind);
            problemAnalyzerData.SetFile(File);

            myElementAnalyzerDispatcher = elementProblemAnalyzerRegistrar.CreateDispatcher(problemAnalyzerData);
        }

        public override void VisitNode(ITreeNode node, IHighlightingConsumer context)
        {
            myElementAnalyzerDispatcher.Run(node, context);
            base.VisitNode(node, context);
        }

        public override void Execute(Action<DaemonStageResult> committer)
        {
            HighlightInFile((file, consumer) => file.ProcessDescendants(this, consumer), committer);
        }
    }
}