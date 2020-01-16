using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class ExpensiveInvocationAnalyzer : PerformanceProblemAnalyzerBase<IInvocationExpression>
    {
        private readonly SolutionAnalysisService mySwa;
        private readonly CallGraphSwaExtensionProvider mySwaExtensionProvider;
        private readonly ExpensiveCodeCallGraphAnalyzer myExpensiveCodeCallGraphAnalyzer;

        public ExpensiveInvocationAnalyzer(SolutionAnalysisService swa,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            ExpensiveCodeCallGraphAnalyzer expensiveCodeCallGraphAnalyzer)
        {
            mySwa = swa;
            mySwaExtensionProvider = callGraphSwaExtensionProvider;
            myExpensiveCodeCallGraphAnalyzer = expensiveCodeCallGraphAnalyzer;
        }

        protected override void Analyze(IInvocationExpression expression, IDaemonProcess daemonProcess, DaemonProcessKind kind,
            IHighlightingConsumer consumer)
        {
            if (PerformanceCriticalCodeStageUtil.IsInvocationExpensive(expression))
            {
                CreateHiglighting(expression, consumer);
            } else if (kind == DaemonProcessKind.GLOBAL_WARNINGS)
            {
                var declaredElement = expression.Reference?.Resolve().DeclaredElement;
                if (declaredElement == null)
                    return;

                var id = mySwa.GetElementId(declaredElement);
                if (!id.HasValue)
                    return;

                if (mySwaExtensionProvider.IsMarkedByCallGraphAnalyzer(myExpensiveCodeCallGraphAnalyzer.Id, id.Value, true))
                {
                    CreateHiglighting(expression, consumer);
                }
            }
        }

        private void CreateHiglighting(IInvocationExpression expression, IHighlightingConsumer consumer)
        {
            consumer.AddHighlighting(new PerformanceInvocationHighlighting(expression, (expression.InvokedExpression as IReferenceExpression)?.Reference));
        }
    }
}