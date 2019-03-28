using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Tooltips;
using JetBrains.Diagnostics;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Host.Features.CodeInsights;
using JetBrains.ReSharper.Host.Features.Icons;
using JetBrains.ReSharper.Host.Platform.CodeInsights;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CSharp.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Rider.Model;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    [SolutionComponent]
    public class RiderUnityHighlightingContributor : UnityHighlightingContributor
    {
        private readonly UnityCodeInsightFieldUsageProvider myFieldUsageProvider;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly ConnectionTracker myConnectionTracker;
        private readonly IconHost myIconHost;

        public RiderUnityHighlightingContributor(ISolution solution, ITextControlManager textControlManager,
            UnityCodeInsightFieldUsageProvider fieldUsageProvider, UnityCodeInsightProvider codeInsightProvider,
            ISettingsStore settingsStore, ConnectionTracker connectionTracker, SolutionAnalysisService swa,
            PerformanceCriticalCodeCallGraphAnalyzer analyzer, IconHost iconHost = null)
            : base(solution, settingsStore, textControlManager, swa, analyzer)
        {
            myFieldUsageProvider = fieldUsageProvider;
            myCodeInsightProvider = codeInsightProvider;
            myConnectionTracker = connectionTracker;
            myIconHost = iconHost;
        }

        public override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string tooltip,
            string displayName, DaemonProcessKind kind, bool addOnlyHotIcon = false)
        {
            if (element is IFieldDeclaration)
                AddHighlighting(consumer, myFieldUsageProvider, element, tooltip, displayName, kind, addOnlyHotIcon);
            else
                AddHighlighting(consumer, myCodeInsightProvider, element, tooltip, displayName, kind, addOnlyHotIcon);
        }

        private void AddHighlighting(IHighlightingConsumer consumer,
            AbstractUnityCodeInsightProvider codeInsightsProvider, ICSharpDeclaration element, string tooltip,
            string displayName, DaemonProcessKind kind, bool addOnlyHotIcon = false)
        {
            if (SettingsStore.GetIndexedValue((CodeInsightsSettings key) => key.DisabledProviders,
                codeInsightsProvider.ProviderId))
            {
                base.AddHighlighting(consumer, element, tooltip, displayName, kind, addOnlyHotIcon);
                return;
            }

            if (SettingsStore.GetValue((UnitySettings key) => key.GutterIconMode) == GutterIconMode.Always)
            {
                base.AddHighlighting(consumer, element, tooltip, displayName, kind, addOnlyHotIcon);
            }

            var isIconHot = IsHotIcon(element, kind);
            if (addOnlyHotIcon && !isIconHot)
                return;
            
            displayName = displayName ?? codeInsightsProvider.DisplayName;
            var declaredElement = element.DeclaredElement;
            if (declaredElement == null || !declaredElement.IsValid())
                return;


            var extraActions = new List<CodeLensEntryExtraActionModel>();
            if (!myConnectionTracker.IsConnectionEstablished())
            {
                extraActions.Add(new CodeLensEntryExtraActionModel("Unity is off", null));
                extraActions.Add(new CodeLensEntryExtraActionModel("Start Unity",
                    AbstractUnityCodeInsightProvider.StartUnityActionId));
            }
            
            var iconId = isIconHot ? InsightUnityIcons.InsightHot.Id : InsightUnityIcons.InsightUnity.Id;
            consumer.AddHighlighting(new UnityCodeInsightsHighlighting(element.GetNameDocumentRange(),
                displayName, tooltip, codeInsightsProvider, declaredElement,
                myIconHost.Transform(iconId), CreateBulbItemsForUnityDeclaration(element), extraActions));
        }

        public override string GetMessageForUnityEventFunction(UnityEventFunction eventFunction)
        {
            return eventFunction.Description ?? "Unity event function";
        }

        public override IEnumerable<BulbMenuItem> CreateAdditionalMenuItem(IDeclaration declaration, UnityApi api, 
            AssetSerializationMode assetSerializationMode, ITextControl textControl)
        {
            var declaredElement = declaration.DeclaredElement;
            if (ShowUsagesInUnityBulbAction.IsAvailableFor(declaredElement, api))
            {
                var action = new ShowUsagesInUnityBulbAction(declaredElement.NotNull("declaredElement != null"), assetSerializationMode,
                    declaration.GetSolution().GetComponent<UnityEditorFindUsageResultCreator>(), myConnectionTracker);
                return new[]
                {
                    new BulbMenuItem(
                        new IntentionAction.MyExecutableProxi(action, Solution, textControl),
                        action.Text, BulbThemedIcons.ContextAction.Id,
                        BulbMenuAnchors.FirstClassContextItems)
                };
            }


            return EmptyList<BulbMenuItem>.Instance;
        }


    }
}