using System;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Highlightings.Rider;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings
{
    [ContainsContextConsumer]
    public class BurstCodeContextHighlighter : ContextHighlighterBase
    {
        [CanBeNull, AsyncContextConsumer]
        public static Action ProcessContext(
            [NotNull] Lifetime lifetime, [NotNull] HighlightingProlongedLifetime prolongedLifetime,
            [NotNull, ContextKey(typeof(ContextHighlighterPsiFileView.ContextKey))] IPsiDocumentRangeView psiDocumentRangeView)
        {
            var isEnabled = GetSettingValue(psiDocumentRangeView, HighlightingSettingsAccessor.ContextExitsHighlightingEnabled);
            
            if (!isEnabled) 
                return null;

            var highlighter = new BurstCodeContextHighlighter();

            return highlighter.GetDataProcessAction(prolongedLifetime, psiDocumentRangeView);
        }
        
        protected override void CollectHighlightings(IPsiDocumentRangeView psiDocumentRangeView, HighlightingsConsumer consumer)
        {
            var view = psiDocumentRangeView.View<CSharpLanguage>();
            var node = view.GetSelectedTreeNode<IFunctionDeclaration>();

            if (node == null)
                return;
            
            var solution = psiDocumentRangeView.Solution;
            var swa = solution.GetComponent<SolutionAnalysisService>();
                
            if (!UnityCallGraphUtil.IsSweaCompleted(swa))
                return;

            var contextProvider = solution.GetComponent<BurstContextProvider>();
            var settingsStore = psiDocumentRangeView.GetSettingsStore();
 
            if (contextProvider.IsContextAvailable == false)
                return;

            if (settingsStore.GetValue((UnitySettings key) => key.BurstCodeHighlightingMode) !=
                BurstCodeHighlightingMode.CurrentMethod)
                return;
            
            var processKind = UnityCallGraphUtil.GetProcessKindForGraph(swa);

            if (contextProvider.HasContext(node, processKind))
                consumer.ConsumeHighlighting(new BurstContextHighlightInfo(node.GetDocumentRange()));
        }
    }
}