using System.Collections.Generic;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class EventHandlerDetector : UnityDeclarationHighlightingProviderBase
    {
        private readonly CallGraphSwaExtensionProvider myCallGraphSwaExtension;
        private readonly UnitySceneDataLocalCache mySceneDataCache;

        public EventHandlerDetector(ISolution solution, SolutionAnalysisService swa, SettingsStore settingsStore,
            CallGraphSwaExtensionProvider callGraphSwaExtension, UnitySceneDataLocalCache sceneDataCache, PerformanceCriticalCodeCallGraphAnalyzer analyzer)
            : base(solution, swa, callGraphSwaExtension, settingsStore, analyzer)
        {
            myCallGraphSwaExtension = callGraphSwaExtension;
            mySceneDataCache = sceneDataCache;
        }

        public override IDeclaredElement Analyze(IDeclaration element, IHighlightingConsumer consumer,
            DaemonProcessKind kind)
        {
            var declaredElement = element is IPropertyDeclaration
                ? ((IProperty) element.DeclaredElement)?.Setter
                : element.DeclaredElement as IMethod;
            if (declaredElement != null && mySceneDataCache.IsEventHandler(declaredElement))
            {
                AddHighlighting(consumer, element as ICSharpDeclaration, "Event handler", "Unity event handler",  kind);
                return declaredElement;
            }

            return null;
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip, DaemonProcessKind kind)
        {
            consumer.AddImplicitConfigurableHighlighting(element);

            var isIconHot = element.HasHotIcon(Swa, myCallGraphSwaExtension, Settings, Analyzer, kind);

            var highlighting = isIconHot
                ? new UnityHotGutterMarkInfo(GetActions(element), element, tooltip)
                : (IHighlighting) new UnityGutterMarkInfo(GetActions(element), element, tooltip);
            consumer.AddHighlighting(highlighting);
        }

        protected override IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration)
        {
            return EnumerableCollection<BulbMenuItem>.Empty;
        }
    }
}