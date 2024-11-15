using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class BurstGutterMarkProvider: BurstProblemAnalyzerBase<IMethodDeclaration>
    {
        private readonly BurstCodeInsights myBurstCodeInsights;
        private readonly IProperty<bool> myBurstEnableIcons;
        
        public BurstGutterMarkProvider(
            Lifetime lifetime,
            IThreading threading,
            IApplicationWideContextBoundSettingStore store,
            BurstCodeInsights burstCodeInsights)
        {
            myBurstEnableIcons = store.BoundSettingsStore.GetValueProperty2(lifetime, (UnitySettings key) => key.EnableIconsForBurstCode, ApartmentForNotifications.Primary(threading));
            myBurstCodeInsights = burstCodeInsights;
        }

        public virtual bool IsGutterMarkEnabled => myBurstEnableIcons.Value;

        protected override bool CheckAndAnalyze(IMethodDeclaration methodDeclaration, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            return false;
        }

        protected override void Analyze(IMethodDeclaration methodDeclaration, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            if (!IsGutterMarkEnabled)
                return;
            
            var items = myBurstCodeInsights.GetBurstActions(methodDeclaration, context);
            
            // Skip methods explicitly marked with BurstCompile attribute
            if(HasBurstAttributeInstance(methodDeclaration))
                return;
            
            var gutterMark = new UnityGutterMarkInfo(items, methodDeclaration, BurstCodeAnalysisUtil.BurstTooltip);
          
            consumer.AddHighlighting(gutterMark);
        }

        protected static bool HasBurstAttributeInstance(IMethodDeclaration methodDeclaration)
        {
            var hasAttributeInstance =
                methodDeclaration.DeclaredElement?.HasAttributeInstance(KnownTypes.BurstCompileAttribute,
                    AttributesSource.Self) ?? true;
            return hasAttributeInstance;
        }
    }
}