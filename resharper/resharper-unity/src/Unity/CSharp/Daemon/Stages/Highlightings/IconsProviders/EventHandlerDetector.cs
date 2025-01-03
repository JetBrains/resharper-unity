using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Explicit;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Implicit;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class EventHandlerDetector : UnityDeclarationHighlightingProviderBase
    {
        protected readonly UnityEventsElementContainer UnityEventsElementContainer;
        private readonly AnimExplicitUsagesContainer myAnimExplicitUsagesContainer;
        [NotNull] private readonly AnimImplicitUsagesContainer myAnimImplicitUsagesContainer;

        public EventHandlerDetector(ISolution solution, IApplicationWideContextBoundSettingStore settingsStore,
                                    UnityEventsElementContainer unityEventsElementContainer,
                                    PerformanceCriticalContextProvider contextProvider,
                                    [NotNull] AnimExplicitUsagesContainer animExplicitUsagesContainer,
                                    [NotNull] AnimImplicitUsagesContainer animImplicitUsagesContainer)
            : base(solution, settingsStore, contextProvider)
        {
            UnityEventsElementContainer = unityEventsElementContainer;
            myAnimExplicitUsagesContainer = animExplicitUsagesContainer;
            myAnimImplicitUsagesContainer = animImplicitUsagesContainer;
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
            var isAnimationEvent = myAnimExplicitUsagesContainer.GetEventUsagesCountFor(method, out _) > 0 || 
                                   myAnimImplicitUsagesContainer.GetEventUsagesCountFor(method, out var expected) > 0 || expected;
            if (isAnimationEvent) AddAnimationEventHighlighting(treeNode, consumer, context);
            return isAnimationEvent;
        }

        private void TryAddAnimationEventHighlightingForPropertyGetter(ITreeNode treeNode,
                                                                       IHighlightingConsumer consumer,
                                                                       IReadOnlyCallGraphContext context,
                                                                       IProperty property)
        {
            var getter = property.Getter;
            if (getter != null && myAnimExplicitUsagesContainer.GetEventUsagesCountFor(getter, out _) > 0)
            {
                AddAnimationEventHighlighting(treeNode, consumer, context);
            }
        }

        private bool TryAddMethodHighlighting(IDeclaration treeNode, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context,
            IMethod method)
        {
            var eventHandlersCount = UnityEventsElementContainer.GetAssetUsagesCount(method, out var estimated);
            var animExplicitCount = myAnimExplicitUsagesContainer.GetEventUsagesCountFor(method, out var estimated2);
            var animImplicitCount = myAnimImplicitUsagesContainer.GetEventUsagesCountFor(method, out var estimated3);
            if (eventHandlersCount == 0 && (animExplicitCount != 0 || animImplicitCount != 0))
            {
                AddAnimationEventHighlighting(treeNode, consumer, context);
                return true;
            }

            if (estimated || estimated2 || estimated3 || animExplicitCount + eventHandlersCount + animImplicitCount > 0)
            {
                AddEventHandlerHighlighting(treeNode, consumer, context);
                return true;
            }
            
            return false;
        }

        private void AddEventHandlerHighlighting([NotNull] ITreeNode treeNode,
                                                 [NotNull] IHighlightingConsumer consumer,
                                                 IReadOnlyCallGraphContext context)
        {
            AddHighlighting(consumer, treeNode as ICSharpDeclaration, Strings.EventHandlerDetector_AddEventHandlerHighlighting_Text, Strings.EventHandlerDetector_AddEventHandlerHighlighting_Tooltip, context);
        }

        private void AddAnimationEventHighlighting([NotNull] ITreeNode treeNode,
                                                   [NotNull] IHighlightingConsumer consumer,
                                                   IReadOnlyCallGraphContext context)
        {
            AddHighlighting(consumer, treeNode as ICSharpDeclaration, Strings.EventHandlerDetector_AddAnimationEventHighlighting_Text, Strings.EventHandlerDetector_AddAnimationEventHighlighting_Tooltip, context);
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text,
            string tooltip, IReadOnlyCallGraphContext context)
        {
            consumer.AddImplicitConfigurableHighlighting(element);
            if (!IconProviderUtil.ShouldShowGutterMarkIcon(SettingsStore.BoundSettingsStore))
                return;

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