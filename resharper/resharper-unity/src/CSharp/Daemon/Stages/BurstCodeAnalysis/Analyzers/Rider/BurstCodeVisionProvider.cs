using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis.AddDiscardAttribute;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.Rider
{
    [SolutionComponent]
    public class BurstCodeVisionProvider : BurstProblemAnalyzerBase<IMethodDeclaration>
    {
        private readonly IApplicationWideContextBoundSettingStore mySettingsStore;
        private readonly ISolution mySolution;
        private readonly BurstCodeInsightProvider myCodeInsightProvider;
        private readonly IconHost myIconHost;
        private readonly ITextControlManager myTextControlManager;

        public BurstCodeVisionProvider(ISolution solution,
            IApplicationWideContextBoundSettingStore store,
            BurstCodeInsightProvider codeInsightProvider, IconHost iconHost)
        {
            mySolution = solution;
            myTextControlManager = mySolution.GetComponent<ITextControlManager>();
            mySettingsStore = store;
            myCodeInsightProvider = codeInsightProvider;
            myIconHost = iconHost;
        }

        protected override bool CheckAndAnalyze(IMethodDeclaration methodDeclaration, IHighlightingConsumer consumer)
        {
            if (consumer == null)
                return false;

            if (!RiderIconProviderUtil.IsCodeVisionEnabled(mySettingsStore.BoundSettingsStore, myCodeInsightProvider.ProviderId,
                () => { }, out _)) return false;

            var declaredElement = methodDeclaration.DeclaredElement;

            myCodeInsightProvider.AddHighlighting(consumer, methodDeclaration, declaredElement, BurstCodeAnalysisUtil.BURST_DISPLAY_NAME,
                BurstCodeAnalysisUtil.BURST_TOOLTIP,
                BurstCodeAnalysisUtil.BURST_DISPLAY_NAME,
                myIconHost.Transform(InsightUnityIcons.InsightUnity.Id),
                GetBurstActions(methodDeclaration),
                null);

            return false;
        }

        private List<BulbMenuItem> GetBurstActions([NotNull] IMethodDeclaration methodDeclaration)
        {
            var result = new List<BulbMenuItem>();
            var textControl = myTextControlManager.LastFocusedTextControl.Value;
            var bulbAction = new AddDiscardAttributeBulbAction(methodDeclaration);
            
            result.Add(new BulbMenuItem(new IntentionAction.MyExecutableProxi(bulbAction, mySolution, textControl),
                bulbAction.Text, BulbThemedIcons.ContextAction.Id, BulbMenuAnchors.FirstClassContextItems));
            
            return result;
        }
    }
}