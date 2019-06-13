using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Daemon;
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
        private readonly PerformanceCriticalCodeCallGraphAnalyzer myAnalyzer;
        protected readonly IContextBoundSettingsStore Settings;

        public UnityDeclarationHighlightingProviderBase(ISolution solution, SolutionAnalysisService swa, SettingsStore settingsStore, 
            PerformanceCriticalCodeCallGraphAnalyzer analyzer)
        {
            Solution = solution;
            Swa = swa;
            myAnalyzer = analyzer;
            Settings = settingsStore.BindToContextTransient(ContextRange.Smart(solution.ToDataContext()));
        }
        
        
        public abstract IDeclaredElement Analyze(IDeclaration treeNode, IHighlightingConsumer consumer,
            DaemonProcessKind kind);
        
        protected virtual void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip, DaemonProcessKind kind)
        {
            consumer.AddHotHighlighting(Swa, element, myAnalyzer, Settings, text, tooltip, kind, GetActions(element));
        }


        protected abstract IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration);
    }
}