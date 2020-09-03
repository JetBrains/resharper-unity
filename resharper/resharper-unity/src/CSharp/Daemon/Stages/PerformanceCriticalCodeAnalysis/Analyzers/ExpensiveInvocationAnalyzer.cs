using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class ExpensiveInvocationAnalyzer : PerformanceProblemAnalyzerBase<IInvocationExpression>
    {
        private readonly CallGraphSwaExtensionProvider mySwaExtensionProvider;
        private readonly ExpensiveCodeMarksProvider myExpensiveCodeMarksProvider;
        private readonly IElementIdProvider myProvider;

        public ExpensiveInvocationAnalyzer(CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            ExpensiveCodeMarksProvider expensiveCodeMarksProvider,
            IElementIdProvider provider)
        {
            mySwaExtensionProvider = callGraphSwaExtensionProvider;
            myExpensiveCodeMarksProvider = expensiveCodeMarksProvider;
            myProvider = provider;
        }

        protected override void Analyze(IInvocationExpression expression, IDaemonProcess daemonProcess, DaemonProcessKind kind,
            IHighlightingConsumer consumer)
        {
            if (PerformanceCriticalCodeStageUtil.IsInvocationExpensive(expression))
            {
                CreateHiglighting(expression, consumer);
            } else if (kind == DaemonProcessKind.GLOBAL_WARNINGS)
            {
                var declaredElement = CallGraphUtil.GetCallee(expression);
                if (declaredElement == null)
                    return;

                var id = myProvider.GetElementId(declaredElement);
                if (!id.HasValue)
                    return;

                if (mySwaExtensionProvider.IsMarkedByCallGraphRootMarksProvider(myExpensiveCodeMarksProvider.Id, true, id.Value))
                {
                    CreateHiglighting(expression, consumer);
                }
            }
        }

        private void CreateHiglighting(IInvocationExpression expression, IHighlightingConsumer consumer)
        {
            consumer.AddHighlighting(new UnityPerformanceInvocationWarning(expression, (expression.InvokedExpression as IReferenceExpression)?.Reference));
        }
    }
}