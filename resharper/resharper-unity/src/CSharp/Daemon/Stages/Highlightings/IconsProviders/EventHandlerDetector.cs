using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class EventHandlerDetector : IUnityDeclarationHiglightingProvider
    {
        private readonly UnityEventHandlerReferenceCache myCache;
        private readonly UnityHighlightingContributor myImplicitUsageHighlightingContributor;

        public EventHandlerDetector([NotNull] UnityApi unityApi, UnityEventHandlerReferenceCache cache,
            UnityHighlightingContributor implicitUsageHighlightingContributor)
        {
            myCache = cache;
            myImplicitUsageHighlightingContributor = implicitUsageHighlightingContributor;
        }

        public IDeclaredElement Analyze(IDeclaration element, IHighlightingConsumer consumer, DaemonProcessKind kind)
        {
            var declaredElement = element is IPropertyDeclaration
                ? ((IProperty) element.DeclaredElement)?.Setter
                : element.DeclaredElement as IMethod;
            if (declaredElement != null && myCache.IsEventHandler(declaredElement))
            {
                myImplicitUsageHighlightingContributor.AddUnityEventHandler(consumer, element as ICSharpDeclaration, "Unity event handler",
                    element is IMethodDeclaration ? "Event handler" : "Implicit usage", kind);
                return declaredElement;
            }

            return null;
        }
    }
}