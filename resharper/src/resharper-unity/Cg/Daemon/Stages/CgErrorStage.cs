using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Stages
{
    [DaemonStage(StagesBefore = new[] {typeof(CollectUsagesStage), typeof(GlobalFileStructureCollectorStage)},
        StagesAfter = new[] {typeof(LanguageSpecificDaemonStage)})]
    public class CgErrorStage : CgDaemonStageBase
    {
        private readonly ElementProblemAnalyzerRegistrar myElementProblemAnalyzerRegistrar;

        public CgErrorStage(ElementProblemAnalyzerRegistrar elementProblemAnalyzerRegistrar)
        {
            myElementProblemAnalyzerRegistrar = elementProblemAnalyzerRegistrar;
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process,
            IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICgFile file)
        {
            return new CgErrorStageProcess(process, processKind, myElementProblemAnalyzerRegistrar, settings, file);
        }

        private class CgErrorStageProcess : CgDaemonStageProcessBase
        {
            private readonly IElementAnalyzerDispatcher myElementAnalyzerDispatcher;

            public CgErrorStageProcess(IDaemonProcess process, DaemonProcessKind processKind,
                ElementProblemAnalyzerRegistrar elementProblemAnalyzerRegistrar, IContextBoundSettingsStore settings,
                ICgFile file)
                : base(process, file, settings)
            {
                var elementProblemAnalyzerData = new ElementProblemAnalyzerData(process, processKind, settings);
                myElementAnalyzerDispatcher = elementProblemAnalyzerRegistrar.CreateDispatcher(elementProblemAnalyzerData);
            }

            public override void VisitNode(ITreeNode element, IHighlightingConsumer consumer)
            {
                myElementAnalyzerDispatcher.Run(element, consumer);
            }
        }
    }
}