using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
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
            consumer.AddHighlighting(new UnityImplicitlyUsedIdentifierHighlighting(declaration.NameIdentifier.GetDocumentRange()));
        }

        public static bool HasHotIcon(this ICSharpDeclaration element, PerformanceCriticalContextProvider contextProvider,
            IContextBoundSettingsStore settingsStore, IReadOnlyCallGraphContext context)
        {
            var declaredElement = element.DeclaredElement;
            
            return declaredElement.HasHotIcon(contextProvider, settingsStore, context);
        }

        public static bool HasHotIcon(this IDeclaredElement element, PerformanceCriticalContextProvider contextProvider,
            IContextBoundSettingsStore settingsStore, IReadOnlyCallGraphContext context)
        {
            if (element == null)
                return false;

            if (!settingsStore.GetValue((UnitySettings key) => key.EnableIconsForPerformanceCriticalCode))
                return false;

            return contextProvider.IsMarkedStage(element, context);
        }
        
        public static void AddHotHighlighting(this IHighlightingConsumer consumer,
                                              PerformanceCriticalContextProvider contextProvider,
                                              ICSharpDeclaration element,
                                              IContextBoundSettingsStore settings, string text,
                                              string tooltip, IReadOnlyCallGraphContext context, IEnumerable<BulbMenuItem> items,
                                              bool onlyHot = false)
        {
            var isIconHot = element.HasHotIcon(contextProvider, settings, context);

            if (onlyHot && !isIconHot)
                return;

            var highlighting = isIconHot
                ? new UnityHotGutterMarkInfo(items, element, tooltip)
                : (IHighlighting) new UnityGutterMarkInfo(items, element, tooltip);
            
            consumer.AddHighlighting(highlighting);
        }
    }
}