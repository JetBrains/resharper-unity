using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class EventHandlerDetector : UnityDeclarationHighlightingProviderBase
    {
        protected readonly UnityEventsElementContainer UnityEventsElementContainer;

        public EventHandlerDetector(ISolution solution, IApplicationWideContextBoundSettingStore settingsStore,
                                    CallGraphSwaExtensionProvider callGraphSwaExtension,
                                    UnityEventsElementContainer unityEventsElementContainer,
                                    PerformanceCriticalCodeCallGraphMarksProvider marksProvider,
                                    IElementIdProvider provider)
            : base(solution, settingsStore, callGraphSwaExtension, marksProvider, provider)
        {
            UnityEventsElementContainer = unityEventsElementContainer;
        }

        public override bool AddDeclarationHighlighting(IDeclaration treeNode, IHighlightingConsumer consumer,
                                                        DaemonProcessKind kind)
        {
            var declaredElement = treeNode.DeclaredElement;
            var method = declaredElement as IMethod;
            if (method is IAccessor)
                return false;

            if (declaredElement is IProperty property)
                method = property.Setter;

            if (method != null && UnityEventsElementContainer.GetAssetUsagesCount(method, out _) > 0)
            {
                AddHighlighting(consumer, treeNode as ICSharpDeclaration, "Event handler", "Unity event handler", kind);
                return true;
            }

            return false;
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip, DaemonProcessKind kind)
        {
            consumer.AddImplicitConfigurableHighlighting(element);

            var isIconHot = element.HasHotIcon(CallGraphSwaExtensionProvider, SettingsStore.BoundSettingsStore, MarksProvider,
                kind, ElementIdProvider);

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