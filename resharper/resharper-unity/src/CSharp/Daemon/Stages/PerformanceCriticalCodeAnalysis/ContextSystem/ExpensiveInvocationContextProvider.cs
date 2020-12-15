using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi;
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
            ExpensiveCodeMarksProvider marksProviderBase,
            SolutionAnalysisService service)
            : base(lifetime, elementIdProvider, applicationWideContextBoundSettingStore, callGraphSwaExtensionProvider,
                marksProviderBase, service)
        {
        }

        public override CallGraphContextElement Context => CallGraphContextElement.EXPENSIVE_CONTEXT;

        protected override bool CheckDeclaredElement(IDeclaredElement element, out bool isMarked)
        {
            if (element is IMethod method && PerformanceCriticalCodeStageUtil.IsInvokedElementExpensive(method))
            {
                isMarked = true;
                return true;
            }

            return base.CheckDeclaredElement(element, out isMarked);
        }
    }
}