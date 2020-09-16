using System;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
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
            if (!isEnabled) return null;

            var highlighter = new BurstCodeContextHighlighter();

            return highlighter.GetDataProcessAction(prolongedLifetime, psiDocumentRangeView);
        }
        
        protected override void CollectHighlightings(IPsiDocumentRangeView psiDocumentRangeView, HighlightingsConsumer consumer)
        {
            var settingsStore = psiDocumentRangeView.GetSettingsStore();
            
            if (!settingsStore.GetValue((UnitySettings key) => key.EnableBurstCodeHighlighting))
                return;

            var view = psiDocumentRangeView.View<CSharpLanguage>();
            var node = view.GetSelectedTreeNode<IFunctionDeclaration>();
            
            if (node != null && !BurstCodeAnalysisUtil.IsBurstContextBannedNode(node))
            {
                var declaredElement = node.DeclaredElement;
                if  (declaredElement == null)
                    return;
                
                var solution = psiDocumentRangeView.Solution;
                var swa = solution.GetComponent<SolutionAnalysisService>();
                if (swa?.Configuration?.Enabled?.Value == false)
                    return;
                var isGlobalStage = swa.Configuration?.Completed?.Value == true;
                var callGraphExtension = solution.GetComponent<CallGraphSwaExtensionProvider>();
                var callGraphAnalyzer = solution.GetComponent<CallGraphBurstMarksProvider>();
                var elementIdProvider = solution.GetComponent<IElementIdProvider>();
                var usageChecker = swa.UsageChecker;
                if (usageChecker == null)
                    return;
                var elementId = elementIdProvider.GetElementId(declaredElement);
                if (!elementId.HasValue)
                    return;

                if (callGraphExtension.IsMarkedByCallGraphRootMarksProvider(callGraphAnalyzer.Id, isGlobalStage, elementId.Value))
                {
                    consumer.ConsumeHighlighting(new UnityBurstContextHighlightInfo(node.GetDocumentRange()));
                }
            }
        }
    }
}