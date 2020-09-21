using System.Collections.Generic;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderUnityCommonIconProvider : UnityCommonIconProvider
    {
        private readonly ISolution mySolution;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly ConnectionTracker myConnectionTracker;
        private readonly IconHost myIconHost;
        private readonly ITextControlManager myTextControlManager;

        public RiderUnityCommonIconProvider(ISolution solution, SettingsStore settingsStore, UnityApi api, UnityCodeInsightProvider codeInsightProvider,
            UnitySolutionTracker solutionTracker, ConnectionTracker connectionTracker, IconHost iconHost, UnityProblemAnalyzerContextSystem contextSystem)
            : base(solution, settingsStore, api, contextSystem)
        {
            mySolution = solution;
            myTextControlManager = mySolution.GetComponent<ITextControlManager>();
            myCodeInsightProvider = codeInsightProvider;
            mySolutionTracker = solutionTracker;
            myConnectionTracker = connectionTracker;
            myIconHost = iconHost;
        }

        public override void AddEventFunctionHighlighting(IHighlightingConsumer consumer, IMethod method, UnityEventFunction eventFunction,
            string text,DaemonProcessKind kind)
        {
            var iconId = method.HasHotIcon(ContextSystem, Settings, kind)
                ? InsightUnityIcons.InsightHot.Id
                : InsightUnityIcons.InsightUnity.Id;
            
            if (RiderIconProviderUtil.IsCodeVisionEnabled(Settings, myCodeInsightProvider.ProviderId,
                () => { base.AddEventFunctionHighlighting(consumer, method, eventFunction, text, kind);}, out var useFallback))
            {
                foreach (var declaration in method.GetDeclarations())
                {
                    if (declaration is ICSharpDeclaration cSharpDeclaration)
                    {
                        if (!useFallback)
                        {
                            consumer.AddImplicitConfigurableHighlighting(cSharpDeclaration);
                        }
                        
                        myCodeInsightProvider.AddHighlighting(consumer, cSharpDeclaration, method, text, eventFunction.Description ?? string.Empty, text,
                            myIconHost.Transform(iconId), GetEventFunctionActions(cSharpDeclaration), RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
                    }
                }
            }
        }

        public override void AddFrequentlyCalledMethodHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration declaration, string text,
            string tooltip, DaemonProcessKind kind)
        {
            var isHot = declaration.HasHotIcon(ContextSystem, Settings, kind);
            if (!isHot)
                return;
            
            if (RiderIconProviderUtil.IsCodeVisionEnabled(Settings, myCodeInsightProvider.ProviderId,
                () => { base.AddFrequentlyCalledMethodHighlighting(consumer, declaration, text, tooltip, kind);}, out var useFallback))
            {

                IEnumerable<BulbMenuItem> actions;
                if (declaration.DeclaredElement is IMethod method && UnityApi.IsEventFunction(method))
                {
                    actions = GetEventFunctionActions(declaration);
                }
                else
                {
                    if (declaration is IMethodDeclaration methodDeclaration)
                    {
                        var bulbAction = new AddExpensiveMethodAttributeBulbAction(methodDeclaration);
                        var textControl = myTextControlManager.LastFocusedTextControl.Value;
                        var result = FixedList.Of(new BulbMenuItem(
                            new IntentionAction.MyExecutableProxi(bulbAction, mySolution, textControl), bulbAction.Text,
                            BulbThemedIcons.ContextAction.Id, BulbMenuAnchors.FirstClassContextItems));
                        actions = result;
                    }
                    else
                    {
                        actions = EmptyList<BulbMenuItem>.Instance;
                    }
                }
                
                myCodeInsightProvider.AddHighlighting(consumer, declaration, declaration.DeclaredElement, text, tooltip, text,
                    myIconHost.Transform(InsightUnityIcons.InsightHot.Id), actions, RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
            }
        }
    }
}