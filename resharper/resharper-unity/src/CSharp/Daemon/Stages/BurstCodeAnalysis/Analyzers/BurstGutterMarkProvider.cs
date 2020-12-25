using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstGutterMarkProvider: BurstProblemAnalyzerBase<IMethodDeclaration>
    {
        private readonly BurstCodeInsights myBurstCodeInsights;
        private readonly IProperty<bool> myBurstEnableIcons;

        
        public BurstGutterMarkProvider(
            Lifetime lifetime,
            IApplicationWideContextBoundSettingStore store,
            BurstCodeInsights burstCodeInsights)
        {
            myBurstEnableIcons = store.BoundSettingsStore.GetValueProperty(lifetime, (UnitySettings key) => key.EnableIconsForBurstCode);
            myBurstCodeInsights = burstCodeInsights;
        }

        protected override bool CheckAndAnalyze(IMethodDeclaration methodDeclaration, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            return false;
        }

        protected override void Analyze(IMethodDeclaration methodDeclaration, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            if (!myBurstEnableIcons.Value)
                return;
            
            var items = myBurstCodeInsights.GetBurstActions(methodDeclaration, context);
            var gutterMark = new UnityGutterMarkInfo(items, methodDeclaration, BurstCodeAnalysisUtil.BURST_TOOLTIP);
          
            consumer.AddHighlighting(gutterMark);
        }
    }
}