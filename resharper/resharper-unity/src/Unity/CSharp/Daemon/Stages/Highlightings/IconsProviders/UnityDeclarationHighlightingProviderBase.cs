using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    public abstract class UnityDeclarationHighlightingProviderBase : IUnityDeclarationHighlightingProvider
    {
        protected readonly ISolution Solution;
        protected readonly PerformanceCriticalContextProvider ContextProvider;
        protected readonly IApplicationWideContextBoundSettingStore SettingsStore;
        
        protected UnityDeclarationHighlightingProviderBase(ISolution solution,
                                                           IApplicationWideContextBoundSettingStore settingsStore,
                                                           PerformanceCriticalContextProvider contextProvider)
        {
            Solution = solution;
            SettingsStore = settingsStore;
            ContextProvider = contextProvider;
        }

        public abstract bool AddDeclarationHighlighting(IDeclaration treeNode, IHighlightingConsumer consumer,
                                                        IReadOnlyCallGraphContext context);

        protected virtual void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip, IReadOnlyCallGraphContext context)
        {
            consumer.AddImplicitConfigurableHighlighting(element);
            
            if (!IconProviderUtil.ShouldShowGutterMarkIcon(SettingsStore.BoundSettingsStore))
                return;
            
            consumer.AddHotHighlighting(ContextProvider, element, SettingsStore.BoundSettingsStore, text, tooltip, context, GetActions(element));
        }


        protected abstract IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration);
    }
}