using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages
{
    [DaemonStage(StagesBefore = new[] { typeof(CollectUsagesStage), typeof(GlobalFileStructureCollectorStage) },
        StagesAfter = new[] { typeof(LanguageSpecificDaemonStage) })]
    public class ShaderLabErrorStage : ShaderLabStageBase
    {
        private readonly ElementProblemAnalyzerRegistrar myElementProblemAnalyzerRegistrar;

        public ShaderLabErrorStage(ElementProblemAnalyzerRegistrar elementProblemAnalyzerRegistrar)
        {
            myElementProblemAnalyzerRegistrar = elementProblemAnalyzerRegistrar;
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, IShaderLabFile file)
        {
            return new ShaderLabErrorStageProcess(process, processKind, myElementProblemAnalyzerRegistrar, settings, file);
        }

        private class ShaderLabErrorStageProcess : ShaderLabDaemonStageProcessBase
        {
            private readonly IElementAnalyzerDispatcher myElementAnalyzerDispatcher;

            public ShaderLabErrorStageProcess(IDaemonProcess process, DaemonProcessKind processKind, ElementProblemAnalyzerRegistrar elementProblemAnalyzerRegistrar, IContextBoundSettingsStore settings, IShaderLabFile file)
                : base(process, settings, file)
            {
                var elementProblemAnalyzerData = new ElementProblemAnalyzerData(file, settings, ElementProblemAnalyzerRunKind.FullDaemon);
                elementProblemAnalyzerData.SetDaemonProcess(process, processKind);
                myElementAnalyzerDispatcher = elementProblemAnalyzerRegistrar.CreateDispatcher(elementProblemAnalyzerData);
            }

            public override void VisitNode(ITreeNode element, IHighlightingConsumer consumer)
            {
                myElementAnalyzerDispatcher.Run(element, consumer);
            }
        }
    }
}
