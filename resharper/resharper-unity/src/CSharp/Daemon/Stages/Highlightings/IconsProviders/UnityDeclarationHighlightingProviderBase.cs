using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    public abstract class UnityDeclarationHighlightingProviderBase : IUnityDeclarationHighlightingProvider
    {
        protected readonly ISolution Solution;
        protected readonly UnityProblemAnalyzerContextSystem ContextSystem;
        protected readonly IContextBoundSettingsStore Settings;
       
        protected UnityDeclarationHighlightingProviderBase(ISolution solution, SettingsStore settingsStore, UnityProblemAnalyzerContextSystem contextSystem)
        {
            Solution = solution;
            ContextSystem = contextSystem;
            Settings = settingsStore.BindToContextTransient(ContextRange.Smart(solution.ToDataContext()));
        }
        
        public abstract bool AddDeclarationHighlighting(IDeclaration treeNode, IHighlightingConsumer consumer,
            DaemonProcessKind kind);
        
        protected virtual void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip, DaemonProcessKind kind)
        {
            consumer.AddImplicitConfigurableHighlighting(element);
            consumer.AddHotHighlighting(ContextSystem, Settings, element, text, tooltip, kind, GetActions(element));
        }


        protected abstract IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration);
    }
}