using System;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CaretDependentFeatures
{
    [ContainsContextConsumer]
    public class PerformanceCriticalCodeContextHighlighter : ContextHighlighterBase
    {
        [CanBeNull, AsyncContextConsumer]
        public static Action ProcessContext(
            Lifetime lifetime, [NotNull] HighlightingProlongedLifetime prolongedLifetime,
            [NotNull, ContextKey(typeof(ContextHighlighterPsiFileView.ContextKey))]
            IPsiDocumentRangeView psiDocumentRangeView)
        {
            var isEnabled = GetSettingValue(psiDocumentRangeView, HighlightingSettingsAccessor.ContextExitsHighlightingEnabled);
            if (!isEnabled)
                return null;

            var highlighter = new PerformanceCriticalCodeContextHighlighter();

            return highlighter.GetDataProcessAction(prolongedLifetime, psiDocumentRangeView);
        }

        protected override void CollectHighlightings(IPsiDocumentRangeView psiDocumentRangeView, HighlightingsConsumer consumer)
        {
            var view = psiDocumentRangeView.View<CSharpLanguage>();
            var node = view.GetSelectedTreeNode<IFunctionDeclaration>();

            if (node == null)
                return;

            var solution = psiDocumentRangeView.Solution;
            var settingsStore = psiDocumentRangeView.GetSettingsStore();

            if (settingsStore.GetValue((UnitySettings key) => key.PerformanceHighlightingMode) !=
                PerformanceHighlightingMode.CurrentMethod)
                return;

            var contextProvider = solution.GetComponent<PerformanceCriticalContextProvider>();
            var swea = solution.GetComponent<SolutionAnalysisService>();
            var declaredElement = node.DeclaredElement;

            if (contextProvider.IsMarkedSweaDependent(declaredElement, swea))
                consumer.ConsumeHighlighting(new UnityPerformanceContextHighlightInfo(node.GetDocumentRange()));
        }
    }
}