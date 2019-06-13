//using System;
//using System.Collections.Generic;
//using JetBrains.Application.Settings;
//using JetBrains.Application.Threading;
//using JetBrains.Collections.Viewable;
//using JetBrains.DataFlow;
//using JetBrains.Lifetimes;
//using JetBrains.Platform.Unity.EditorPluginModel;
//using JetBrains.ProjectModel;
//using JetBrains.ReSharper.Daemon;
//using JetBrains.ReSharper.Daemon.CodeInsights;
//using JetBrains.ReSharper.Feature.Services.Daemon;
//using JetBrains.ReSharper.Host.Platform.CodeInsights;
//using JetBrains.ReSharper.Host.Platform.Icons;
//using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
//using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
//using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
//using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
//using JetBrains.ReSharper.Plugins.Unity.Settings;
//using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
//using JetBrains.ReSharper.Psi;
//using JetBrains.ReSharper.Psi.CSharp.Tree;
//using JetBrains.ReSharper.Psi.Tree;
//using JetBrains.ReSharper.Resources.Shell;
//using JetBrains.Rider.Model;
//using JetBrains.TextControl;
//using JetBrains.Threading;
//
//namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
//{
//    [SolutionComponent]
//    public class RiderUnityHighlightingContributor : UnityHighlightingContributor
//    {
//        private readonly UnityCodeInsightFieldUsageProvider myFieldUsageProvider;
//        private readonly UnityCodeInsightProvider myCodeInsightProvider;
//        private readonly ConnectionTracker myConnectionTracker;
//        private readonly UnitySolutionTracker mySolutionTracker;
//        private readonly UnityApi myUnityApi;
//        private readonly UnityEventHandlerReferenceCache myHandlerReferenceCache;
//        private readonly IconHost myIconHost;
//
//        public RiderUnityHighlightingContributor(Lifetime lifetime, ISolution solution, ITextControlManager textControlManager,
//            UnityCodeInsightFieldUsageProvider fieldUsageProvider, UnityCodeInsightProvider codeInsightProvider,
//            ISettingsStore settingsStore, ConnectionTracker connectionTracker, SolutionAnalysisService swa, IShellLocks locks,
//            PerformanceCriticalCodeCallGraphAnalyzer analyzer, UnitySolutionTracker solutionTracker, UnityApi unityApi, 
//            UnityEventHandlerReferenceCache handlerReferenceCache, IconHost iconHost = null)
//            : base(solution, settingsStore, textControlManager, swa, analyzer)
//        {
//            myFieldUsageProvider = fieldUsageProvider;
//            myCodeInsightProvider = codeInsightProvider;
//            myConnectionTracker = connectionTracker;
//            mySolutionTracker = solutionTracker;
//            myUnityApi = unityApi;
//            myHandlerReferenceCache = handlerReferenceCache;
//            myIconHost = iconHost;
//            var invalidateDaemonResultGroupingEvent = locks.GroupingEvents.CreateEvent(lifetime,
//                "UnityHiglightingContributor::InvalidateDaemonResults", TimeSpan.FromSeconds(5), Rgc.Guarded,
//                () =>
//                {
//                    solution.GetComponent<IDaemon>().Invalidate();
//                });
//            myConnectionTracker.State.Change.Advise_HasNew(lifetime, value =>
//            {
//                var old = value.HasOld ? value.Old : UnityEditorState.Disconnected;
//                var @new = value.New;
//                
//                // disconnect -> ??? -> disconnect is rarely case, we do not check it
//                // connected -> ??? -> connected is the most important case
//                if (old != UnityEditorState.Disconnected &&  @new != UnityEditorState.Disconnected)
//                {
//                    invalidateDaemonResultGroupingEvent.CancelIncoming();
//                }
//                else if (old == UnityEditorState.Disconnected && @new != UnityEditorState.Disconnected ||
//                         @new == UnityEditorState.Disconnected && old != UnityEditorState.Disconnected)
//                {
//                    invalidateDaemonResultGroupingEvent.FireIncoming();
//                }
//            });
//        }
//
//        public override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string tooltip,
//            string displayName, DaemonProcessKind kind, bool addOnlyHotIcon = false)
//        {
//            if (element is IFieldDeclaration)
//                AddHighlighting(consumer, myFieldUsageProvider, element, tooltip, displayName, kind, addOnlyHotIcon);
//            else
//                AddHighlighting(consumer, myCodeInsightProvider, element, tooltip, displayName, kind, addOnlyHotIcon);
//        }
//
//        private void AddHighlighting(IHighlightingConsumer consumer,
//            AbstractUnityCodeInsightProvider codeInsightsProvider, ICSharpDeclaration element, string tooltip,
//            string displayName, DaemonProcessKind kind, bool addOnlyHotIcon = false)
//        {
//            if (SettingsStore.GetIndexedValue((CodeInsightsSettings key) => key.DisabledProviders,
//                codeInsightsProvider.ProviderId))
//            {
//                base.AddHighlighting(consumer, element, tooltip, displayName, kind, addOnlyHotIcon);
//                return;
//            }
//
//            if (SettingsStore.GetValue((UnitySettings key) => key.GutterIconMode) == GutterIconMode.Always)
//            {
//                base.AddHighlighting(consumer, element, tooltip, displayName, kind, addOnlyHotIcon);
//            }
//
//            var isIconHot = IsHotIcon(element, kind);
//            if (addOnlyHotIcon && !isIconHot)
//                return;
//            
//            displayName = displayName ?? codeInsightsProvider.DisplayName;
//            var declaredElement = element.DeclaredElement;
//            if (declaredElement == null || !declaredElement.IsValid())
//                return;
//
//            // Since 19.2, Rider will show new code vision for scripts which will show 
//            // asset usages
//            if (myUnityApi.IsDescendantOfMonoBehaviour(declaredElement as ITypeElement)
//                || declaredElement is IMethod method && !myUnityApi.IsEventFunction(method) && 
//                myHandlerReferenceCache.IsEventHandler(method))
//            {
//                return;
//            }
//            
//
//            var extraActions = new List<CodeLensEntryExtraActionModel>();
//            if (mySolutionTracker.IsUnityProject.HasTrueValue() && !myConnectionTracker.IsConnectionEstablished())
//            {
//                extraActions.Add(new CodeLensEntryExtraActionModel("Unity is not running", null));
//                extraActions.Add(new CodeLensEntryExtraActionModel("Start Unity Editor",
//                    AbstractUnityCodeInsightProvider.StartUnityActionId));
//            }
//
//            var iconId = isIconHot ? InsightUnityIcons.InsightHot.Id : InsightUnityIcons.InsightUnity.Id;
//            codeInsightsProvider.AddHighlighting(consumer, element, declaredElement, displayName, tooltip, displayName,
//                myIconHost.Transform(iconId), CreateBulbItemsForUnityDeclaration(element), extraActions);
//
//        }
//
//        public override string GetMessageForUnityEventFunction(UnityEventFunction eventFunction)
//        {
//            return eventFunction.Description ?? "Unity event function";
//        }
//    }
//}