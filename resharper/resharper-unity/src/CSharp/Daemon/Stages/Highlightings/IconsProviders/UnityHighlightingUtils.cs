using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
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

        public static bool HasHotIcon(this ICSharpDeclaration element,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, IContextBoundSettingsStore settingsStore,
            PerformanceCriticalCodeMarksProvider marksProvider, DaemonProcessKind kind, IElementIdProvider provider)
        {
            var declaredElement = element.DeclaredElement;
            if (declaredElement == null)
                return false;

            return declaredElement.HasHotIcon(callGraphSwaExtensionProvider, settingsStore, marksProvider, kind, provider);
        }
        
        public static bool HasHotIcon(this IDeclaredElement element,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, IContextBoundSettingsStore settingsStore,
            PerformanceCriticalCodeMarksProvider marksProvider, DaemonProcessKind kind, IElementIdProvider provider)
        {
            if (!settingsStore.GetValue((UnitySettings key) => key.EnableIconsForPerformanceCriticalCode))
                return false;

            if (!settingsStore.GetValue((UnitySettings key) => key.EnablePerformanceCriticalCodeHighlighting))
                return false;

            var id = provider.GetElementId(element);
            if (!id.HasValue)
                return false;

            return callGraphSwaExtensionProvider.IsMarkedByCallGraphRootMarksProvider(marksProvider.Id, 
                kind == DaemonProcessKind.GLOBAL_WARNINGS, id.Value);
        }

        public static void AddHotHighlighting(this IHighlightingConsumer consumer,
            CallGraphSwaExtensionProvider swaExtensionProvider, ICSharpDeclaration element, PerformanceCriticalCodeMarksProvider marksProvider,
            IContextBoundSettingsStore settings, string text,
            string tooltip, DaemonProcessKind kind, IEnumerable<BulbMenuItem> items, IElementIdProvider provider, bool onlyHot = false)
        {
            var isIconHot = element.HasHotIcon(swaExtensionProvider, settings, marksProvider, kind, provider);
            if (onlyHot && !isIconHot)
                return;
            
            var highlighting = isIconHot
                ? new UnityHotGutterMarkInfo(items, element, tooltip)
                : (IHighlighting) new UnityGutterMarkInfo(items, element, tooltip);
            consumer.AddHighlighting(highlighting);
        }
    }
}