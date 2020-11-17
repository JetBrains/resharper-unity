using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem
{
    [SolutionComponent]
    public sealed class ExpensiveInvocationContextProvider : PerformanceAnalysisContextProviderBase
    {
        public ExpensiveInvocationContextProvider(
            Lifetime lifetime,
            IApplicationWideContextBoundSettingStore applicationWideContextBoundSettingStore,
            IElementIdProvider elementIdProvider,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            ExpensiveCodeMarksProvider marksProviderBase)
            : base(lifetime, elementIdProvider, applicationWideContextBoundSettingStore, callGraphSwaExtensionProvider,
                marksProviderBase, ExpensiveCodeMarksProvider.MarkId)
        {
        }

        public override CallGraphContextElement Context => CallGraphContextElement.EXPENSIVE_CONTEXT;

        protected override bool IsMarkedFast(IDeclaredElement declaredElement) =>
            PerformanceCriticalCodeStageUtil.IsInvokedElementExpensive(declaredElement as IMethod);
    }
}