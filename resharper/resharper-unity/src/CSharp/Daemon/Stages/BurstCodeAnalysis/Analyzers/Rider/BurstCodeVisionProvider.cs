using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.Rider
{
    public interface IBurstCodeVisionMenuItemProvider : ICallGraphCodeVisionMenuItemProvider
    {
    }
    
    [SolutionComponent]
    public sealed class BurstCodeVisionProvider : BurstProblemAnalyzerBase<IMethodDeclaration>
    {
        private readonly IApplicationWideContextBoundSettingStore mySettingsStore;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly IconHost myIconHost;
        private readonly IEnumerable<IBurstCodeVisionMenuItemProvider> myBulbProviders;
        private readonly ITextControlManager myTextControlManager;

        public BurstCodeVisionProvider(ITextControlManager textControlManager,
                                       IApplicationWideContextBoundSettingStore store,
                                       UnityCodeInsightProvider codeInsightProvider,
                                       IconHost iconHost,
                                       IEnumerable<IBurstCodeVisionMenuItemProvider> bulbProviders)
        {
            myTextControlManager = textControlManager;
            mySettingsStore = store;
            myCodeInsightProvider = codeInsightProvider;
            myIconHost = iconHost;
            myBulbProviders = bulbProviders;
        }

        protected override void Analyze(IMethodDeclaration methodDeclaration,
            IHighlightingConsumer consumer, IReadOnlyContext context)
        {
            var boundStore = mySettingsStore.BoundSettingsStore;
            var providerId = myCodeInsightProvider.ProviderId;

            if (!RiderIconProviderUtil.IsCodeVisionEnabled(boundStore, providerId, () => { }, out _))
                return;

            var declaredElement = methodDeclaration.DeclaredElement;
            var iconModel = myIconHost.Transform(InsightUnityIcons.InsightUnity.Id);
            var actions = GetBurstActions(methodDeclaration, context);

            myCodeInsightProvider.AddHighlighting(consumer, methodDeclaration, declaredElement,
                BurstCodeAnalysisUtil.BURST_DISPLAY_NAME,
                BurstCodeAnalysisUtil.BURST_TOOLTIP,
                BurstCodeAnalysisUtil.BURST_DISPLAY_NAME,
                iconModel,
                actions,
                extraActions: null);
        }

        protected override bool CheckAndAnalyze(IMethodDeclaration methodDeclaration, IHighlightingConsumer consumer,
            IReadOnlyContext context)
        {
            return false;
        }

        [NotNull]
        [ItemNotNull]
        private IEnumerable<BulbMenuItem> GetBurstActions([NotNull] IMethodDeclaration methodDeclaration, IReadOnlyContext context)
        {
            var result = new CompactList<BulbMenuItem>();
            var textControl = myTextControlManager.LastFocusedTextControl.Value;
            
            foreach (var bulbProvider in myBulbProviders)
            {
                var menuItems = bulbProvider.GetMenuItems(methodDeclaration, textControl, context);
                
                foreach(var item in menuItems)
                    result.Add(item);
            } 
            
            return result;
        }
    }
}