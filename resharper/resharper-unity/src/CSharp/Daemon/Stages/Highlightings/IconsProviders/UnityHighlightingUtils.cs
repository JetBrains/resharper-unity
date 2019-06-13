using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    public static class UnityHighlightingUtils
    {
        public static void AddImplicitConfigurableHighlighting(this IHighlightingConsumer consumer,
            ICSharpDeclaration declaration)
        {
            consumer.AddHighlighting(
                new UnityImplicitlyUsedIdentifierHighlighting(declaration.NameIdentifier.GetDocumentRange()));
        }

        public static bool HasHotIcon(this ICSharpDeclaration element, SolutionAnalysisService swa,
            IContextBoundSettingsStore settingsStore,
            PerformanceCriticalCodeCallGraphAnalyzer analyzer, DaemonProcessKind kind)
        {
            if (!settingsStore.GetValue((UnitySettings key) => key.EnableIconsForPerformanceCriticalCode))
                return false;

            if (!settingsStore.GetValue((UnitySettings key) => key.EnablePerformanceCriticalCodeHighlighting))
                return false;

            var declaredElement = element.DeclaredElement;
            if (declaredElement == null)
                return false;

            var usageChecker = swa.UsageChecker;
            if (usageChecker == null)
                return false;

            var id = swa.GetElementId(declaredElement, true);
            if (!id.HasValue)
                return false;

            return usageChecker.IsMarkedByCallGraphAnalyzer(analyzer.AnalyzerId, id.Value,
                kind == DaemonProcessKind.GLOBAL_WARNINGS);
        }

        public static void AddHotHighlighting(this IHighlightingConsumer consumer, SolutionAnalysisService swa,
            ICSharpDeclaration element, PerformanceCriticalCodeCallGraphAnalyzer analyzer,
            IContextBoundSettingsStore settings, string text,
            string tooltip, DaemonProcessKind kind, IEnumerable<BulbMenuItem> items)
        {
            consumer.AddImplicitConfigurableHighlighting(element);

            var isIconHot = element.HasHotIcon(swa, settings, analyzer, kind);

            var highlighting = isIconHot
                ? new UnityHotGutterMarkInfo(items, element, tooltip)
                : (IHighlighting) new UnityGutterMarkInfo(items, element, tooltip);
            consumer.AddHighlighting(highlighting);
        }
    }
}