using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Tooltips;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Host.Features.CodeInsights;
using JetBrains.ReSharper.Host.Features.Icons;
using JetBrains.ReSharper.Host.Platform.CodeInsights;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Rider.Model;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    [SolutionComponent]
    public class RiderUnityImplicitUsageHighlightingContributor : UnityImplicitUsageHighlightingContributor
    {
        private readonly UnityCodeInsightFieldUsageProvider myFieldUsageProvider;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly ConnectionTracker myConnectionTracker;
        private readonly IconHost myIconHost;

        public RiderUnityImplicitUsageHighlightingContributor(ISolution solution, ITextControlManager textControlManager,
            UnityCodeInsightFieldUsageProvider fieldUsageProvider,  UnityCodeInsightProvider codeInsightProvider,
            ISettingsStore settingsStore, ConnectionTracker connectionTracker, IconHost iconHost = null)
            : base(solution, settingsStore, textControlManager)
        {
            myFieldUsageProvider = fieldUsageProvider;
            myCodeInsightProvider = codeInsightProvider;
            myConnectionTracker = connectionTracker;
            myIconHost = iconHost;
        }
        
        public override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string tooltip)
        {
            switch (element)
            {
                case IFieldDeclaration _:
                    AddHighlighting(consumer, myFieldUsageProvider, element, tooltip, "Set by Unity");
                    break;
                case IClassLikeDeclaration _:
                    AddHighlighting(consumer, myCodeInsightProvider, element, tooltip, "Scripting component");
                    break;
                case IMethodDeclaration _:
                    AddHighlighting(consumer, myCodeInsightProvider, element, tooltip, "Event function");
                    break;
                default:
                    AddHighlighting(consumer, myCodeInsightProvider, element, tooltip, "Implicit usage");
                    break;
            }
        }
        
        private void AddHighlighting(IHighlightingConsumer consumer, AbstractUnityCodeInsightProvider codeInsightsProvider, ICSharpDeclaration element, string tooltip, string displayName)
        {
            if (SettingsStore.GetIndexedValue((CodeInsightsSettings key) => key.DisabledProviders, codeInsightsProvider.ProviderId))
            {
                base.AddHighlighting(consumer, element, tooltip);
                return;
            }

            if (SettingsStore.GetValue((UnitySettings key) => key.GutterIconMode) == GutterIconMode.Always)
            {
                base.AddHighlighting(consumer, element, tooltip);
            }
            
            displayName = displayName ?? codeInsightsProvider.DisplayName;
            var declaredElement = element.DeclaredElement;
            if (declaredElement == null || !declaredElement.IsValid())
                return;


            var extraActions = new List<CodeLensEntryExtraActionModel>();
            if (!myConnectionTracker.IsConnectionEstablished())
            {
                extraActions.Add(new CodeLensEntryExtraActionModel("Unity is off", null));
                extraActions.Add(new CodeLensEntryExtraActionModel("Start Unity", AbstractUnityCodeInsightProvider.StartUnityActionId));
            }
            
            consumer.AddHighlighting(new UnityCodeInsightsHighlighting(element.GetNameDocumentRange(),
                displayName, displayName, codeInsightsProvider, declaredElement, 
                myIconHost.Transform(codeInsightsProvider.IconId), CreateBulbItemsForUnityDeclaration(element), extraActions));
        }


        public override IEnumerable<BulbMenuItem> CreateAdditionalMenuItem(IDeclaration declaration, UnityApi api, ITextControl textControl)
        {
            var declaredElement = declaration.DeclaredElement;
            if (declaredElement != null && (declaredElement is IMethod method && !api.IsEventFunction(method) ||
                                            declaration is IClassDeclaration))
            {
                var action = new UnityFindUsagesNavigationAction(declaredElement, 
                    declaration.GetSolution().GetComponent<UnityEditorFindRequestCreator>(),  myConnectionTracker);
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

        internal class UnityFindUsagesNavigationAction : BulbActionBase
        {
            private readonly IDeclaredElement myDeclaredElement;
            private readonly UnityEditorFindRequestCreator myCreator;
            [NotNull] private readonly ConnectionTracker myTracker;

            public UnityFindUsagesNavigationAction([NotNull]IDeclaredElement method, [NotNull]UnityEditorFindRequestCreator creator, [NotNull] ConnectionTracker tracker)
            {
                myDeclaredElement = method;
                myCreator = creator;
                myTracker = tracker;
            }
            
            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                if (!myTracker.IsConnectionEstablished())
                {
                    return textControl => ShowTooltip(textControl, "Unity is not running");
                }
                    
                myCreator.CreateRequestToUnity(myDeclaredElement, null, true);
                return null;
            }

            public override string Text => "Show usages in Unity";
            
        }
    }
}