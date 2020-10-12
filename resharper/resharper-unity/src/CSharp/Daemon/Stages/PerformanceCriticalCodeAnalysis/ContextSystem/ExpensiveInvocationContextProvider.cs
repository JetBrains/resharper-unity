using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem
{
    [SolutionComponent]
    public sealed class ExpensiveInvocationContextProvider : CallGraphContextProviderBase
    {
        private readonly IProperty<bool> myIsPerformanceAnalysisEnabledProperty;

        public ExpensiveInvocationContextProvider(
            Lifetime lifetime,
            IApplicationWideContextBoundSettingStore applicationWideContextBoundSettingStore,
            IElementIdProvider elementIdProvider,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            ExpensiveCodeMarksProvider marksProviderBase)
            : base(elementIdProvider, callGraphSwaExtensionProvider, marksProviderBase)
        {
            myIsPerformanceAnalysisEnabledProperty =
                applicationWideContextBoundSettingStore.BoundSettingsStore.GetValueProperty(lifetime,
                    (UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting);
        }

        public override CallGraphContextElement Context => CallGraphContextElement.EXPENSIVE_CONTEXT;

        public override bool IsContextAvailable => myIsPerformanceAnalysisEnabledProperty.Value;

        public override bool IsCalleeMarked(ICSharpExpression expression, DaemonProcessKind processKind)
        {
            if (IsContextAvailable == false)
                return false;
            
            if (expression is IInvocationExpression invocationExpression &&
                PerformanceCriticalCodeStageUtil.IsInvocationExpensive(invocationExpression))
                return true;

            return base.IsCalleeMarked(expression, processKind);
        }

        public override bool IsMarked(IDeclaredElement declaredElement, DaemonProcessKind processKind)
        {
            if (declaredElement is IMethod method && 
                PerformanceCriticalCodeStageUtil.IsInvokedElementExpensive(method))
                return true;

            return base.IsMarked(declaredElement, processKind);
        }
    }
}