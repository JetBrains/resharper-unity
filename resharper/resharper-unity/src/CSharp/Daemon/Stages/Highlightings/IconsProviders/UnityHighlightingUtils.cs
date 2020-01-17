using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
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
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, IContextBoundSettingsStore settingsStore,
            PerformanceCriticalCodeCallGraphAnalyzer analyzer, DaemonProcessKind kind)
        {
            var declaredElement = element.DeclaredElement;
            if (declaredElement == null)
                return false;

            return declaredElement.HasHotIcon(swa, callGraphSwaExtensionProvider, settingsStore, analyzer, kind);
        }
        
        public static bool HasHotIcon(this IDeclaredElement element, SolutionAnalysisService swa,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, IContextBoundSettingsStore settingsStore,
            PerformanceCriticalCodeCallGraphAnalyzer analyzer, DaemonProcessKind kind)
        {
            if (!settingsStore.GetValue((UnitySettings key) => key.EnableIconsForPerformanceCriticalCode))
                return false;

            if (!settingsStore.GetValue((UnitySettings key) => key.EnablePerformanceCriticalCodeHighlighting))
                return false;

            var usageChecker = swa.UsageChecker;
            if (usageChecker == null)
                return false;

            var id = swa.GetElementId(element);
            if (!id.HasValue)
                return false;

            return callGraphSwaExtensionProvider.IsMarkedByCallGraphAnalyzer(analyzer.Id, id.Value,
                kind == DaemonProcessKind.GLOBAL_WARNINGS);
        }

        public static void AddHotHighlighting(this IHighlightingConsumer consumer, SolutionAnalysisService swa,
            CallGraphSwaExtensionProvider swaExtensionProvider, ICSharpDeclaration element, PerformanceCriticalCodeCallGraphAnalyzer analyzer,
            IContextBoundSettingsStore settings, string text,
            string tooltip, DaemonProcessKind kind, IEnumerable<BulbMenuItem> items, bool onlyHot = false)
        {
            var isIconHot = element.HasHotIcon(swa, swaExtensionProvider, settings, analyzer, kind);
            if (onlyHot && !isIconHot)
                return;
            
            var highlighting = isIconHot
                ? new UnityHotGutterMarkInfo(items, element, tooltip)
                : (IHighlighting) new UnityGutterMarkInfo(items, element, tooltip);
            consumer.AddHighlighting(highlighting);
        }
    }
}