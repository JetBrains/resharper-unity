using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    public abstract class UnityDeclarationHighlightingProviderBase : IUnityDeclarationHighlightingProvider
    {
        protected readonly ISolution Solution;
        protected readonly CallGraphSwaExtensionProvider CallGraphSwaExtensionProvider;
        protected readonly PerformanceCriticalCodeCallGraphMarksProvider MarksProvider;
        protected readonly IContextBoundSettingsStore Settings;
        private readonly IElementIdProvider myProvider;

        public UnityDeclarationHighlightingProviderBase(ISolution solution, CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, 
            SettingsStore settingsStore, PerformanceCriticalCodeCallGraphMarksProvider marksProvider, IElementIdProvider provider)
        {
            Solution = solution;
            CallGraphSwaExtensionProvider = callGraphSwaExtensionProvider;
            MarksProvider = marksProvider;
            Settings = settingsStore.BindToContextTransient(ContextRange.Smart(solution.ToDataContext()));
            myProvider = provider;
        }
        
        public abstract bool AddDeclarationHighlighting(IDeclaration treeNode, IHighlightingConsumer consumer,
            DaemonProcessKind kind);
        
        protected virtual void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip, DaemonProcessKind kind)
        {
            consumer.AddImplicitConfigurableHighlighting(element);
            consumer.AddHotHighlighting(CallGraphSwaExtensionProvider, element, MarksProvider, Settings, text, tooltip, kind, GetActions(element), myProvider);
        }


        protected abstract IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration);
    }
}