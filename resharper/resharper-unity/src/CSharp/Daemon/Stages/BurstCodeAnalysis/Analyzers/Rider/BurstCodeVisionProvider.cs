using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Features.Inspections.CallHierarchy;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis.AddDiscardAttribute;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph.UnityCallGraphUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.Rider
{
    [SolutionComponent]
    public sealed class BurstCodeVisionProvider : BurstProblemAnalyzerBase<IMethodDeclaration>
    {
        private readonly IApplicationWideContextBoundSettingStore mySettingsStore;
        private readonly ISolution mySolution;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly IconHost myIconHost;
        private readonly ITextControlManager myTextControlManager;

        public BurstCodeVisionProvider(ISolution solution,
                                       IApplicationWideContextBoundSettingStore store,
                                       UnityCodeInsightProvider codeInsightProvider,
                                       IconHost iconHost)
        {
            mySolution = solution;
            myTextControlManager = mySolution.GetComponent<ITextControlManager>();
            mySettingsStore = store;
            myCodeInsightProvider = codeInsightProvider;
            myIconHost = iconHost;
        }

        protected override void Analyze(IMethodDeclaration methodDeclaration,
                                                IDaemonProcess daemonProcess,
                                                DaemonProcessKind kind,
                                        IHighlightingConsumer consumer)
        {
            var boundStore = mySettingsStore.BoundSettingsStore;
            var providerId = myCodeInsightProvider.ProviderId;

            if (!RiderIconProviderUtil.IsCodeVisionEnabled(boundStore, providerId, () => { }, out _))
                return;

            var declaredElement = methodDeclaration.DeclaredElement;
            var iconModel = myIconHost.Transform(InsightUnityIcons.InsightUnity.Id);
            var actions = GetBurstActions(methodDeclaration);

            myCodeInsightProvider.AddHighlighting(consumer, methodDeclaration, declaredElement,
                BurstCodeAnalysisUtil.BURST_DISPLAY_NAME,
                BurstCodeAnalysisUtil.BURST_TOOLTIP,
                BurstCodeAnalysisUtil.BURST_DISPLAY_NAME,
                iconModel,
                actions,
                extraActions: null);
        }

        protected override bool CheckAndAnalyze(IMethodDeclaration methodDeclaration, IHighlightingConsumer consumer)
        {
            return false;
        }

        private IEnumerable<BulbMenuItem> GetBurstActions([NotNull] IMethodDeclaration methodDeclaration)
        {
            var result = new CompactList<BulbMenuItem>();
            var textControl = myTextControlManager.LastFocusedTextControl.Value;
            var addDiscardAttributeAction = new AddDiscardAttributeBulbAction(methodDeclaration);

            AddAction(addDiscardAttributeAction, BulbThemedIcons.ContextAction.Id);

            return result;

            void AddAction(IBulbAction action, IconId iconId)
            {
                var menuItem = BulbActionToMenuItem(action, textControl, mySolution, iconId);

                result.Add(menuItem);
            }
        }
    }
}