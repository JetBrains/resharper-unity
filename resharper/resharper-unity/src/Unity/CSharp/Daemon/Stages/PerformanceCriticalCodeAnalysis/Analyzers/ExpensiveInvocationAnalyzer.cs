using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class ExpensiveInvocationAnalyzer : PerformanceProblemAnalyzerBase<IInvocationExpression>
    {
        private readonly ExpensiveInvocationContextProvider myContextProvider;

        public ExpensiveInvocationAnalyzer(ExpensiveInvocationContextProvider contextProvider)
        {
            myContextProvider = contextProvider;
        }

        protected override void Analyze(IInvocationExpression expression, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            var callee = CallGraphUtil.GetCallee(expression);
            
            if (PerformanceCriticalCodeStageUtil.IsInvokedElementExpensive(callee as IMethod) || myContextProvider.IsMarkedStage(callee, context))
                CreateHighlighting(expression, consumer);
        }

        private static void CreateHighlighting(IInvocationExpression expression, IHighlightingConsumer consumer)
        {
            consumer.AddHighlighting(new UnityPerformanceInvocationWarning(expression,
                (expression.InvokedExpression as IReferenceExpression)?.Reference));
        }
    }
}