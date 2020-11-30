using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis.AddExpensiveComment;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.PerformanceAnalysis;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderUnityCommonIconProvider : UnityCommonIconProvider
    {
        private readonly ISolution mySolution;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly BackendUnityHost myBackendUnityHost;
        private readonly IconHost myIconHost;
        private readonly ExpensiveInvocationContextProvider myExpensiveInvocationContextProvider;
        private readonly ITextControlManager myTextControlManager;

        public RiderUnityCommonIconProvider(ISolution solution,
                                            IApplicationWideContextBoundSettingStore settingsStore,
                                            UnityApi api,
                                            UnityCodeInsightProvider codeInsightProvider,
                                            UnitySolutionTracker solutionTracker,
                                            BackendUnityHost backendUnityHost,
                                            IconHost iconHost, PerformanceCriticalContextProvider contextProvider,
                                            ExpensiveInvocationContextProvider expensiveInvocationContextProvider)
            : base(solution, api, settingsStore, contextProvider)
        {
            mySolution = solution;
            myTextControlManager = mySolution.GetComponent<ITextControlManager>();
            myCodeInsightProvider = codeInsightProvider;
            mySolutionTracker = solutionTracker;
            myBackendUnityHost = backendUnityHost;
            myIconHost = iconHost;
            myExpensiveInvocationContextProvider = expensiveInvocationContextProvider;
        }

        public override void AddEventFunctionHighlighting(IHighlightingConsumer consumer, IMethod method, UnityEventFunction eventFunction,
                                                          string text,DaemonProcessKind kind)
        {
            var iconId = method.HasHotIcon(ContextProvider, SettingsStore.BoundSettingsStore, kind)
                ? InsightUnityIcons.InsightHot.Id
                : InsightUnityIcons.InsightUnity.Id;

            if (RiderIconProviderUtil.IsCodeVisionEnabled(SettingsStore.BoundSettingsStore, myCodeInsightProvider.ProviderId,
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
                            myIconHost.Transform(iconId), GetEventFunctionActions(cSharpDeclaration), RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myBackendUnityHost));
                    }
                }
            }
        }

        public override void AddFrequentlyCalledMethodHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration declaration, string text,
            string tooltip, DaemonProcessKind kind)
        {
            var isHot = declaration.HasHotIcon(ContextProvider, SettingsStore.BoundSettingsStore, kind);
            if (!isHot)
                return;

            if (RiderIconProviderUtil.IsCodeVisionEnabled(SettingsStore.BoundSettingsStore, myCodeInsightProvider.ProviderId,
                () => { base.AddFrequentlyCalledMethodHighlighting(consumer, declaration, text, tooltip, kind);}, out _))
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
                        var textControl = myTextControlManager.LastFocusedTextControl.Value;
                        var compactList = new CompactList<BulbMenuItem>();
                        var performanceDisableAction =
                            new PerformanceAnalysisDisableByCommentBulbAction(methodDeclaration);
                        var performanceDisableBulbItem = new BulbMenuItem(
                            new IntentionAction.MyExecutableProxi(performanceDisableAction, mySolution, textControl),
                            performanceDisableAction.Text,
                            BulbThemedIcons.ContextAction.Id, BulbMenuAnchors.FirstClassContextItems);
                        compactList.Add(performanceDisableBulbItem);

                        if (!myExpensiveInvocationContextProvider.HasContext(methodDeclaration, kind))
                        {
                            var expensiveBulbAction = new AddExpensiveCommentBulbAction(methodDeclaration);
                            var expensiveBulbItem = new BulbMenuItem(
                                new IntentionAction.MyExecutableProxi(expensiveBulbAction, mySolution, textControl),
                                expensiveBulbAction.Text,
                                BulbThemedIcons.ContextAction.Id, BulbMenuAnchors.FirstClassContextItems);

                            compactList.Add(expensiveBulbItem);
                        }

                        actions = compactList;
                    }
                    else
                    {
                        actions = EmptyList<BulbMenuItem>.Instance;
                    }
                }

                myCodeInsightProvider.AddHighlighting(consumer, declaration, declaration.DeclaredElement, text, tooltip, text,
                    myIconHost.Transform(InsightUnityIcons.InsightHot.Id), actions, RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myBackendUnityHost));
            }
        }
    }
}