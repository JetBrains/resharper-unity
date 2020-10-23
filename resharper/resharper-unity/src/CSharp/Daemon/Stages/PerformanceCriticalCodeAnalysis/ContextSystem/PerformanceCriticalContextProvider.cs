using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem
{
    [SolutionComponent]
    public class PerformanceCriticalContextProvider : CallGraphContextProviderBase
    {
        public override CallGraphContextElement Context => CallGraphContextElement.PERFORMANCE_CRITICAL_CONTEXT;

        public override bool IsContextAvailable => myIsPerformanceAnalysisEnabledProperty.Value;

        public override bool HasContext(IDeclaration declaration, DaemonProcessKind processKind)
        {
            var functionDeclaration = declaration as IFunctionDeclaration;
            var hasComment = UnityCallGraphUtil.HasAnalysisComment(functionDeclaration, PerformanceCriticalCodeMarksProvider.MarkId, ReSharperControlConstruct.Kind.Restore);

            if (hasComment)
                return true;
            
            return base.HasContext(declaration, processKind);
        }

        public override bool IsMarked(IDeclaredElement declaredElement, DaemonProcessKind processKind)
        {
            if (IsContextAvailable == false)
                return false;
            
            if (PerformanceCriticalCodeStageUtil.IsPerformanceCriticalRootMethod(declaredElement))
                return true;
            
            return base.IsMarked(declaredElement, processKind);
        }

        private readonly IProperty<bool> myIsPerformanceAnalysisEnabledProperty;

        public PerformanceCriticalContextProvider(
            Lifetime lifetime,
            IElementIdProvider elementIdProvider,
            IApplicationWideContextBoundSettingStore applicationWideContextBoundSettingStore,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            PerformanceCriticalCodeMarksProvider marksProvider)
            : base(elementIdProvider, callGraphSwaExtensionProvider, marksProvider)
        {
            myIsPerformanceAnalysisEnabledProperty =
                applicationWideContextBoundSettingStore.BoundSettingsStore.GetValueProperty(lifetime,
                    (UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting);
        }
    }
}