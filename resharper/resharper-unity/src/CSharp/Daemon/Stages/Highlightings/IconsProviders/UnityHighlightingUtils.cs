using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
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

        public static bool HasHotIcon(this ICSharpDeclaration element, UnityProblemAnalyzerContextSystem contextSystem,
            IContextBoundSettingsStore settingsStore, DaemonProcessKind kind)
        {
            var declaredElement = element.DeclaredElement;
            
            return declaredElement.HasHotIcon(contextSystem, settingsStore, kind);
        }

        public static bool HasHotIcon(this IDeclaredElement element, UnityProblemAnalyzerContextSystem contextSystem,
            IContextBoundSettingsStore settingsStore, DaemonProcessKind kind)
        {
            if (element == null)
                return false;

            return contextSystem
                .GetContextProvider(settingsStore, UnityProblemAnalyzerContextElement.PERFORMANCE_CONTEXT)
                .IsMarked(element, kind);
        }

        public static void AddHotHighlighting(this IHighlightingConsumer consumer,
            UnityProblemAnalyzerContextSystem contextProvider, IContextBoundSettingsStore settingsStore,
            ICSharpDeclaration element, string text,
            string tooltip, DaemonProcessKind kind, IEnumerable<BulbMenuItem> items, bool onlyHot = false)
        {
            var isIconHot = element.HasHotIcon(contextProvider, settingsStore, kind);

            if (onlyHot && !isIconHot)
                return;

            var highlighting = isIconHot
                ? new UnityHotGutterMarkInfo(items, element, tooltip)
                : (IHighlighting) new UnityGutterMarkInfo(items, element, tooltip);
            consumer.AddHighlighting(highlighting);
        }
    }
}