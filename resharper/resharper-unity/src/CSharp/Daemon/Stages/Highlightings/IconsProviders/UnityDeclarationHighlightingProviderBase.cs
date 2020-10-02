using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    public abstract class UnityDeclarationHighlightingProviderBase : IUnityDeclarationHighlightingProvider
    {
        protected readonly ISolution Solution;
        protected readonly CallGraphSwaExtensionProvider CallGraphSwaExtensionProvider;
        protected readonly PerformanceCriticalCodeCallGraphMarksProvider MarksProvider;
        protected readonly IApplicationWideContextBoundSettingStore SettingsStore;
        protected readonly IElementIdProvider ElementIdProvider;

        protected UnityDeclarationHighlightingProviderBase(ISolution solution,
                                                           IApplicationWideContextBoundSettingStore settingsStore,
                                                           CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
                                                           PerformanceCriticalCodeCallGraphMarksProvider marksProvider,
                                                           IElementIdProvider provider)
        {
            Solution = solution;
            CallGraphSwaExtensionProvider = callGraphSwaExtensionProvider;
            MarksProvider = marksProvider;
            SettingsStore = settingsStore;
            ElementIdProvider = provider;
        }

        public abstract bool AddDeclarationHighlighting(IDeclaration treeNode, IHighlightingConsumer consumer,
                                                        DaemonProcessKind kind);

        protected virtual void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip, DaemonProcessKind kind)
        {
            consumer.AddImplicitConfigurableHighlighting(element);
            consumer.AddHotHighlighting(CallGraphSwaExtensionProvider, element, MarksProvider,
                SettingsStore.BoundSettingsStore, text, tooltip, kind, GetActions(element), ElementIdProvider);
        }


        protected abstract IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration);
    }
}