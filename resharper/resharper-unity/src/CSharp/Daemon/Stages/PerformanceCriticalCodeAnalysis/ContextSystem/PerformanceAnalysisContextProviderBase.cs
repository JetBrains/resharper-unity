using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem
{
    public abstract class PerformanceAnalysisContextProviderBase : CallGraphContextProviderBase
    {
        protected readonly IProperty<bool> IsPerformanceAnalysisEnabledProperty;
        protected readonly string MarkId;

        protected PerformanceAnalysisContextProviderBase(
            Lifetime lifetime,
            IElementIdProvider elementIdProvider,
            IApplicationWideContextBoundSettingStore applicationWideContextBoundSettingStore,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            CallGraphRootMarksProviderBase marksProvider,
            string markId)
            : base(elementIdProvider, callGraphSwaExtensionProvider, marksProvider)
        {
            MarkId = markId;
            IsPerformanceAnalysisEnabledProperty =
                applicationWideContextBoundSettingStore.BoundSettingsStore.GetValueProperty(lifetime,
                    (UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting);
        }
        
        public override bool HasContext(IDeclaration declaration, DaemonProcessKind processKind)
        {
            var functionDeclaration = declaration as IFunctionDeclaration;
            var hasComment = UnityCallGraphUtil.HasAnalysisComment(functionDeclaration,
                MarkId, ReSharperControlConstruct.Kind.Restore);

            return hasComment || base.HasContext(declaration, processKind);
        }

        public override bool IsMarked(IDeclaredElement declaredElement, DaemonProcessKind processKind)
        {
            if (IsContextAvailable == false)
                return false;
            
            return IsMarkedFast(declaredElement) || base.IsMarked(declaredElement, processKind);
        }

        /// <summary>
        /// Method for checking declared element on local stage
        /// </summary>
        /// <param name="declaredElement"></param>
        /// <returns></returns>
        protected virtual bool IsMarkedFast([CanBeNull] IDeclaredElement declaredElement) => false;

        public override bool IsContextAvailable => IsPerformanceAnalysisEnabledProperty.Value;
    }
}