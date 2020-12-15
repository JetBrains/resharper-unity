using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
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
    public interface IPerformanceAnalysisCodeVisionMenuItemProvider : ICallGraphCodeVisionMenuItemProvider
    {
    }
    
    [SolutionComponent]
    public sealed class RiderUnityCommonIconProvider : UnityCommonIconProvider
    {
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly BackendUnityHost myBackendUnityHost;
        private readonly IconHost myIconHost;
        private readonly ITextControlManager myTextControlManager;
        private readonly IEnumerable<IPerformanceAnalysisCodeVisionMenuItemProvider> myMenuItemProviders;

        public RiderUnityCommonIconProvider(ISolution solution,
                                            IApplicationWideContextBoundSettingStore settingsStore,
                                            UnityApi api,
                                            UnityCodeInsightProvider codeInsightProvider,
                                            UnitySolutionTracker solutionTracker,
                                            BackendUnityHost backendUnityHost,
                                            IconHost iconHost, PerformanceCriticalContextProvider contextProvider,
                                            IEnumerable<IPerformanceAnalysisCodeVisionMenuItemProvider> menuItemProviders)
            : base(solution, api, settingsStore, contextProvider)
        {
            myTextControlManager = solution.GetComponent<ITextControlManager>();
            myCodeInsightProvider = codeInsightProvider;
            mySolutionTracker = solutionTracker;
            myBackendUnityHost = backendUnityHost;
            myIconHost = iconHost;
            myMenuItemProviders = menuItemProviders;
        }

        public override void AddEventFunctionHighlighting(IHighlightingConsumer consumer, 
                                                          IMethod method, 
                                                          UnityEventFunction eventFunction,
                                                          string text, 
                                                          DaemonProcessKind kind)
        {
            var boundStore = SettingsStore.BoundSettingsStore;
            var providerId = myCodeInsightProvider.ProviderId;
            void Fallback() => base.AddEventFunctionHighlighting(consumer, method, eventFunction, text, kind);
            
            if (!RiderIconProviderUtil.IsCodeVisionEnabled(boundStore, providerId, Fallback, out var useFallback))
                return;

            var iconId = method.HasHotIcon(PerformanceContextProvider, boundStore, kind)
                ? InsightUnityIcons.InsightHot.Id
                : InsightUnityIcons.InsightUnity.Id;
            var iconModel = myIconHost.Transform(iconId);
            var extraActions = RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myBackendUnityHost);
            
            foreach (var declaration in method.GetDeclarations())
            {
                if (!(declaration is ICSharpDeclaration cSharpDeclaration))
                    continue;
                
                if (!useFallback)
                    consumer.AddImplicitConfigurableHighlighting(cSharpDeclaration);

                var actions = GetEventFunctionActions(cSharpDeclaration);
                
                myCodeInsightProvider.AddHighlighting(consumer, cSharpDeclaration, method, text, 
                    eventFunction.Description ?? string.Empty, text, iconModel, actions, extraActions);
            }
        }

        public override void AddFrequentlyCalledMethodHighlighting(IHighlightingConsumer consumer,
            ICSharpDeclaration cSharpDeclaration, string text, string tooltip, DaemonProcessKind processKind)
        { 
            var boundStore = SettingsStore.BoundSettingsStore;
            
            if (!cSharpDeclaration.HasHotIcon(PerformanceContextProvider, boundStore, processKind))
                return;

            void Fallback() => base.AddFrequentlyCalledMethodHighlighting(consumer, cSharpDeclaration, text, tooltip, processKind);
            var providerId = myCodeInsightProvider.ProviderId;
            
            if (!RiderIconProviderUtil.IsCodeVisionEnabled(boundStore, providerId, Fallback, out _))
                return;

            var actions = GetActions(cSharpDeclaration, processKind);
            var extraActions = RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myBackendUnityHost);
            var iconModel = myIconHost.Transform(InsightUnityIcons.InsightHot.Id);

            myCodeInsightProvider.AddHighlighting(consumer, cSharpDeclaration, cSharpDeclaration.DeclaredElement, 
                text, tooltip, text, iconModel, actions, extraActions);
        }
        
        private IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration, DaemonProcessKind kind)
        {
            if (declaration.DeclaredElement is IMethod method && UnityApi.IsEventFunction(method))
                return GetEventFunctionActions(declaration);
                
            if (!(declaration is IMethodDeclaration methodDeclaration))
                return EmptyList<BulbMenuItem>.Instance;
                    
            var textControl = myTextControlManager.LastFocusedTextControl.Value;
            var result = new CompactList<BulbMenuItem>();

            foreach (var provider in myMenuItemProviders)
            {
                var item = provider.GetMenuItem(methodDeclaration, textControl, kind);    
                
                if (item != null)
                    result.Add(item);
            }

            return result;
        }
    }
}