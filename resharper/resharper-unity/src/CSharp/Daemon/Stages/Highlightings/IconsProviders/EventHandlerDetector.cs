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
                                                        IReadOnlyCallGraphContext context)
        {
            var declaredElement = treeNode.DeclaredElement;
            var method = declaredElement as IMethod;
            if (method is IAccessor) 
                return TryAddAnimationEventHighlightingForAccessorMethod(treeNode, consumer, context, method);

            if (declaredElement is IProperty property)
            {
                TryAddAnimationEventHighlightingForPropertyGetter(treeNode, consumer, context, property);
                method = property.Setter;
            }
            
            return method != null && TryAddMethodHighlighting(treeNode, consumer, context, method);
        }

        private bool TryAddAnimationEventHighlightingForAccessorMethod(ITreeNode treeNode,
                                                                       IHighlightingConsumer consumer,
                                                                       IReadOnlyCallGraphContext context,
                                                                       IDeclaredElement method)
        {
            var isAnimationEvent = myAnimationEventUsagesContainer.GetEventUsagesCountFor(method, out _) > 0;
            if (isAnimationEvent) AddAnimationEventHighlighting(treeNode, consumer, context);
            return isAnimationEvent;
        }

        private void TryAddAnimationEventHighlightingForPropertyGetter(ITreeNode treeNode,
                                                                       IHighlightingConsumer consumer,
                                                                       IReadOnlyCallGraphContext context,
                                                                       IProperty property)
        {
            var getter = property.Getter;
            if (getter != null && myAnimationEventUsagesContainer.GetEventUsagesCountFor(getter, out _) > 0)
            {
                AddAnimationEventHighlighting(treeNode, consumer, context);
            }
        }

        private bool TryAddMethodHighlighting(IDeclaration treeNode, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context,
                                              IMethod method)
        {
            var eventHandlersCount = UnityEventsElementContainer.GetAssetUsagesCount(method, out _);
            var animationEventsCount = myAnimationEventUsagesContainer.GetEventUsagesCountFor(method, out _);
            if (eventHandlersCount + animationEventsCount <= 0) return false;
            if (eventHandlersCount != 0 && animationEventsCount == 0)
                AddEventHandlerHighlighting(treeNode, consumer, context);
            if (eventHandlersCount == 0 && animationEventsCount != 0)
                AddAnimationEventHighlighting(treeNode, consumer, context);
            AddAnimationEventAndEventHandlerHighlighting(treeNode, consumer, context);
            return true;
        }

        private void AddEventHandlerHighlighting([NotNull] ITreeNode treeNode,
                                                 [NotNull] IHighlightingConsumer consumer,
                                                 IReadOnlyCallGraphContext context)
        {
            AddHighlighting(consumer, treeNode as ICSharpDeclaration, "Event handler", "Unity event handler", context);
        }

        private void AddAnimationEventHighlighting([NotNull] ITreeNode treeNode,
                                                   [NotNull] IHighlightingConsumer consumer,
                                                   IReadOnlyCallGraphContext context)
        {
            AddHighlighting(consumer, treeNode as ICSharpDeclaration, "Animation event", "Unity animation event", context);
        }
        
        private void AddAnimationEventAndEventHandlerHighlighting([NotNull] ITreeNode treeNode,
                                                                  [NotNull] IHighlightingConsumer consumer,
                                                                  IReadOnlyCallGraphContext context)
        {
            AddHighlighting(consumer, treeNode as ICSharpDeclaration, "Animation event and event handler",
                "Unity animation event and Unity animation event", context);
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip, IReadOnlyCallGraphContext context)
        {
            consumer.AddImplicitConfigurableHighlighting(element);

            var isIconHot = element.HasHotIcon(ContextProvider, SettingsStore.BoundSettingsStore, context);

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