using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class RiderUnityCommonIconProvider : UnityCommonIconProvider
    {
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly ConnectionTracker myConnectionTracker;
        private readonly IconHost myIconHost;
        private readonly IElementIdProvider myProvider;

        public RiderUnityCommonIconProvider(ISolution solution,
                                            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
                                            IApplicationWideContextBoundSettingStore settingsStore,
                                            PerformanceCriticalCodeCallGraphMarksProvider marksProvider, UnityApi api,
                                            UnityCodeInsightProvider codeInsightProvider,
                                            UnitySolutionTracker solutionTracker, ConnectionTracker connectionTracker,
                                            IconHost iconHost, IElementIdProvider provider)
            : base(solution, api, callGraphSwaExtensionProvider, settingsStore, marksProvider, provider)
        {
            myCodeInsightProvider = codeInsightProvider;
            mySolutionTracker = solutionTracker;
            myConnectionTracker = connectionTracker;
            myIconHost = iconHost;
            myProvider = provider;
        }

        public override void AddEventFunctionHighlighting(IHighlightingConsumer consumer, IMethod method, UnityEventFunction eventFunction,
                                                          string text,DaemonProcessKind kind)
        {
            var iconId = method.HasHotIcon(CallGraphSwaExtensionProvider, SettingsStore.BoundSettingsStore, MarksProvider, kind, myProvider)
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
                            myIconHost.Transform(iconId), GetEventFunctionActions(cSharpDeclaration), RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
                    }
                }
            }
        }

        public override void AddFrequentlyCalledMethodHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration declaration, string text,
            string tooltip, DaemonProcessKind kind)
        {
            var isHot = declaration.HasHotIcon(CallGraphSwaExtensionProvider, SettingsStore.BoundSettingsStore, MarksProvider, kind, myProvider);
            if (!isHot)
                return;

            if (RiderIconProviderUtil.IsCodeVisionEnabled(SettingsStore.BoundSettingsStore, myCodeInsightProvider.ProviderId,
                () => { base.AddFrequentlyCalledMethodHighlighting(consumer, declaration, text, tooltip, kind);}, out var useFallback))
            {
                IEnumerable<BulbMenuItem> actions;
                if (declaration.DeclaredElement is IMethod method && UnityApi.IsEventFunction(method))
                {
                    actions = GetEventFunctionActions(declaration);
                }
                else
                {
                    actions = EmptyList<BulbMenuItem>.Instance;
                }

                myCodeInsightProvider.AddHighlighting(consumer, declaration, declaration.DeclaredElement, text, tooltip, text,
                    myIconHost.Transform(InsightUnityIcons.InsightHot.Id), actions, RiderIconProviderUtil.GetExtraActions(mySolutionTracker, myConnectionTracker));
            }
        }
    }
}