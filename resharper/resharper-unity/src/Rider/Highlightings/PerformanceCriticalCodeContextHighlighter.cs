using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings
{  
    [ContainsContextConsumer]
    public class PerformanceCriticalCodeContextHighlighter : ContextHighlighterBase
    {
        [CanBeNull, AsyncContextConsumer]
        public static Action ProcessContext(
            [NotNull] Lifetime lifetime, [NotNull] HighlightingProlongedLifetime prolongedLifetime,
            [NotNull, ContextKey(typeof(ContextHighlighterPsiFileView.ContextKey))] IPsiDocumentRangeView psiDocumentRangeView)
        {
            var isEnabled = GetSettingValue(psiDocumentRangeView, HighlightingSettingsAccessor.ContextExitsHighlightingEnabled);
            if (!isEnabled) return null;

            var highlighter = new PerformanceCriticalCodeContextHighlighter();

            return highlighter.GetDataProcessAction(prolongedLifetime, psiDocumentRangeView);
        }
        
        protected override void CollectHighlightings(IPsiDocumentRangeView psiDocumentRangeView, HighlightingsConsumer consumer)
        {
            var settingsStore = psiDocumentRangeView.GetSettingsStore();
            
            if (!settingsStore.GetValue((UnitySettings key) => key.EnablePerformanceCriticalCodeHighlighting))
                return;

            if (settingsStore.GetValue((UnitySettings key) => key.PerformanceHighlightingMode) != PerformanceHighlightingMode.CurrentMethod)
                return;
            
            var view = psiDocumentRangeView.View<CSharpLanguage>();
            var node = view.GetSelectedTreeNode<IFunctionDeclaration>();
            
            if (node != null)
            {
                var declaredElement = node.DeclaredElement;
                if  (declaredElement == null)
                    return;
                
                var solution = psiDocumentRangeView.Solution;
                var swa = solution.GetComponent<SolutionAnalysisService>();
                var callGraphExtension = solution.GetComponent<CallGraphSwaExtensionProvider>();
                var callGraphAnalyzer = solution.GetComponent<PerformanceCriticalCodeCallGraphAnalyzer>();
                var usageChecker = swa.UsageChecker;
                if (usageChecker == null)
                    return;
                var elementId = swa.GetElementId(declaredElement);
                if (!elementId.HasValue)
                    return;

                if (callGraphExtension.IsMarkedByCallGraphAnalyzer(callGraphAnalyzer.Id, elementId.Value))
                {
                    consumer.ConsumeHighlighting(new PerformanceContextHiglighting(node.GetDocumentRange()));
                }
            }
        }
    }
}