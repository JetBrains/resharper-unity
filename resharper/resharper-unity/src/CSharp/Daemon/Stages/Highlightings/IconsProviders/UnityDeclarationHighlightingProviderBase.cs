using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    public abstract class UnityDeclarationHighlightingProviderBase : IUnityDeclarationHighlightingProvider
    {
        protected readonly ISolution Solution;
        protected readonly SolutionAnalysisService Swa;
        protected readonly CallGraphSwaExtensionProvider CallGraphSwaExtensionProvider;
        protected readonly PerformanceCriticalCodeCallGraphAnalyzer Analyzer;
        protected readonly IContextBoundSettingsStore Settings;

        public UnityDeclarationHighlightingProviderBase(ISolution solution, SolutionAnalysisService swa, CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, 
            SettingsStore settingsStore, PerformanceCriticalCodeCallGraphAnalyzer analyzer)
        {
            Solution = solution;
            Swa = swa;
            CallGraphSwaExtensionProvider = callGraphSwaExtensionProvider;
            Analyzer = analyzer;
            Settings = settingsStore.BindToContextTransient(ContextRange.Smart(solution.ToDataContext()));
        }
        
        public abstract bool AddDeclarationHighlighting(IDeclaration treeNode, IHighlightingConsumer consumer,
            DaemonProcessKind kind);
        
        protected virtual void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip, DaemonProcessKind kind)
        {
            consumer.AddImplicitConfigurableHighlighting(element);
            consumer.AddHotHighlighting(Swa, CallGraphSwaExtensionProvider, element, Analyzer, Settings, text, tooltip, kind, GetActions(element));
        }


        protected abstract IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration);
    }
}