using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.Rider
{
    [SolutionComponent]
    public class BurstCodeVisionProvider : BurstProblemAnalyzerBase<IMethodDeclaration>
    {
        private readonly IContextBoundSettingsStore mySettingsStore;
        private readonly ISolution mySolution;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly IconHost myIconHost;
        private readonly ITextControlManager myTextControlManager;
        public const string BURST_DISPLAY_NAME = BURST_TOOLTIP;
        public const string BURST_TOOLTIP = "Burst compiled code";

        public BurstCodeVisionProvider(ISolution solution,
            SettingsStore settingsStore,
            UnityCodeInsightProvider codeInsightProvider, IconHost iconHost)
        {
            mySolution = solution;
            myTextControlManager = mySolution.GetComponent<ITextControlManager>();
            mySettingsStore = settingsStore.BindToContextTransient(ContextRange.Smart(solution.ToDataContext()));
            myCodeInsightProvider = codeInsightProvider;
            myIconHost = iconHost;
        }

        protected override bool CheckAndAnalyze(IMethodDeclaration methodDeclaration, IHighlightingConsumer consumer)
        {
            if (consumer == null)
                return false;

            if (!RiderIconProviderUtil.IsCodeVisionEnabled(mySettingsStore, myCodeInsightProvider.ProviderId,
                () => { }, out _)) return false;

            var declaredElement = methodDeclaration.DeclaredElement;

            myCodeInsightProvider.AddHighlighting(consumer, methodDeclaration, declaredElement, BURST_DISPLAY_NAME,
                BURST_TOOLTIP,
                BURST_DISPLAY_NAME,
                myIconHost.Transform(InsightUnityIcons.InsightUnity.Id),
                GetBurstActions(methodDeclaration),
                null);

            return false;
        }

        private List<BulbMenuItem> GetBurstActions(IMethodDeclaration methodDeclaration)
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