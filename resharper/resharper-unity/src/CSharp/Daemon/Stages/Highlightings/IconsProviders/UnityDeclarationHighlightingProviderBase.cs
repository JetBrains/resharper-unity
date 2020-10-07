using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    public abstract class UnityDeclarationHighlightingProviderBase : IUnityDeclarationHighlightingProvider
    {
        protected readonly ISolution Solution;
        protected readonly UnityProblemAnalyzerContextSystem ContextSystem;
        protected readonly IApplicationWideContextBoundSettingStore SettingsStore;
        
        protected UnityDeclarationHighlightingProviderBase(ISolution solution,
                                                           IApplicationWideContextBoundSettingStore settingsStore,
                                                           UnityProblemAnalyzerContextSystem contextSystem)
        {
            Solution = solution;
            SettingsStore = settingsStore;
            ContextSystem = contextSystem;
        }

        public abstract bool AddDeclarationHighlighting(IDeclaration treeNode, IHighlightingConsumer consumer,
                                                        DaemonProcessKind kind);

        protected virtual void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip, DaemonProcessKind kind)
        {
            consumer.AddImplicitConfigurableHighlighting(element);
            consumer.AddHotHighlighting(ContextSystem, element, SettingsStore.BoundSettingsStore, text, tooltip, kind, GetActions(element));
        }


        protected abstract IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration);
    }
}