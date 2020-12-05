using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimationEventsUsages;
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
        private readonly AnimationEventUsagesContainer myAnimationEventUsagesContainer;
        
        public EventHandlerDetector(ISolution solution, IApplicationWideContextBoundSettingStore settingsStore,
                                    UnityEventsElementContainer unityEventsElementContainer,
                                    PerformanceCriticalContextProvider contextProvider,
                                    [NotNull] AnimationEventUsagesContainer animationEventUsagesContainer)
            : base(solution, settingsStore, contextProvider)
        {
            UnityEventsElementContainer = unityEventsElementContainer;
            myAnimationEventUsagesContainer = animationEventUsagesContainer;
        }

        public override bool AddDeclarationHighlighting(IDeclaration treeNode, IHighlightingConsumer consumer,
                                                        DaemonProcessKind kind)
        {
            var declaredElement = treeNode.DeclaredElement;
            var method = declaredElement as IMethod;
            if (method is IAccessor) 
                return TryAddAnimationEventHighlightingForAccessorMethod(treeNode, consumer, kind, method);

            if (declaredElement is IProperty property)
            {
                TryAddAnimationEventHighlightingForPropertyGetter(treeNode, consumer, kind, property);
                method = property.Setter;
            }
            
            return method != null && TryAddMethodHighlighting(treeNode, consumer, kind, method);
        }

        private bool TryAddAnimationEventHighlightingForAccessorMethod(ITreeNode treeNode,
                                                                       IHighlightingConsumer consumer,
                                                                       DaemonProcessKind kind,
                                                                       IDeclaredElement method)
        {
            var isAnimationEvent = myAnimationEventUsagesContainer.GetEventUsagesCountFor(method) > 0;
            if (isAnimationEvent) AddAnimationEventHighlighting(treeNode, consumer, kind);
            return isAnimationEvent;
        }

        private void TryAddAnimationEventHighlightingForPropertyGetter(ITreeNode treeNode,
                                                                       IHighlightingConsumer consumer,
                                                                       DaemonProcessKind kind,
                                                                       IProperty property)
        {
            var getter = property.Getter;
            if (getter != null && myAnimationEventUsagesContainer.GetEventUsagesCountFor(getter) > 0)
            {
                AddAnimationEventHighlighting(treeNode, consumer, kind);
            }
        }

        private bool TryAddMethodHighlighting(IDeclaration treeNode, IHighlightingConsumer consumer, DaemonProcessKind kind,
                                              IMethod method)
        {
            var eventHandlersCount = UnityEventsElementContainer.GetAssetUsagesCount(method, out _);
            var animationEventsCount = myAnimationEventUsagesContainer.GetEventUsagesCountFor(method);
            if (eventHandlersCount + animationEventsCount <= 0) return false;
            if (eventHandlersCount != 0 && animationEventsCount == 0)
                AddEventHandlerHighlighting(treeNode, consumer, kind);
            if (eventHandlersCount == 0 && animationEventsCount != 0)
                AddAnimationEventHighlighting(treeNode, consumer, kind);
            AddAnimationEventAndEventHandlerHighlighting(treeNode, consumer, kind);
            return true;
        }

        private void AddEventHandlerHighlighting([NotNull] ITreeNode treeNode,
                                                 [NotNull] IHighlightingConsumer consumer,
                                                 DaemonProcessKind kind)
        {
            AddHighlighting(consumer, treeNode as ICSharpDeclaration, "Event handler", "Unity event handler", kind);
        }

        private void AddAnimationEventHighlighting([NotNull] ITreeNode treeNode,
                                                   [NotNull] IHighlightingConsumer consumer,
                                                   DaemonProcessKind kind)
        {
            AddHighlighting(consumer, treeNode as ICSharpDeclaration, "Animation event", "Unity animation event", kind);
        }
        
        private void AddAnimationEventAndEventHandlerHighlighting([NotNull] ITreeNode treeNode,
                                                                  [NotNull] IHighlightingConsumer consumer,
                                                                  DaemonProcessKind kind)
        {
            AddHighlighting(consumer, treeNode as ICSharpDeclaration, "Animation event and event handler",
                "Unity animation event and Unity animation event", kind);
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip, DaemonProcessKind kind)
        {
            consumer.AddImplicitConfigurableHighlighting(element);

            var isIconHot = element.HasHotIcon(ContextProvider, SettingsStore.BoundSettingsStore, kind);

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