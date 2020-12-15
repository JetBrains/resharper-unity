using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem
{
    public abstract class PerformanceAnalysisContextProviderBase : CallGraphContextProviderBase
    {
        private readonly IProperty<bool> myIsPerformanceAnalysisEnabledProperty;

        protected PerformanceAnalysisContextProviderBase(
            Lifetime lifetime,
            IElementIdProvider elementIdProvider,
            IApplicationWideContextBoundSettingStore applicationWideContextBoundSettingStore,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            PerformanceAnalysisRootMarksProviderBase marksProvider,
            SolutionAnalysisService service)
            : base(elementIdProvider, callGraphSwaExtensionProvider, marksProvider, service)
        {
            myIsPerformanceAnalysisEnabledProperty =
                applicationWideContextBoundSettingStore.BoundSettingsStore.GetValueProperty(lifetime,
                    (UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting);
        }

        protected override bool CheckDeclaration(IDeclaration declaration, out bool isMarked)
        {
            if (PerformanceAnalysisRootMarksProviderBase.HasPerformanceBanComment(declaration))
            {
                isMarked = false;
                return true;
            }
            
            return base.CheckDeclaration(declaration, out isMarked);
        }

        public override bool IsContextAvailable => myIsPerformanceAnalysisEnabledProperty.Value;
    }
}