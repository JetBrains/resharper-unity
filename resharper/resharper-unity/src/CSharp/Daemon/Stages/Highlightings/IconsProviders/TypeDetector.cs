using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class TypeDetector : IUnityDeclarationHiglightingProvider
    {
        private readonly UnityApi myUnityApi;
        private readonly UnityHighlightingContributor myContributor;

        public TypeDetector(UnityApi unityApi, UnityHighlightingContributor contributor)
        {
            myUnityApi = unityApi;
            myContributor = contributor;
        }

        public IDeclaredElement Analyze(IDeclaration node, IHighlightingConsumer consumer, DaemonProcessKind kind)
        {
            if (!(node is IClassLikeDeclaration element)) 
                return null;
            
            var typeElement = element.DeclaredElement;
            if (typeElement != null)
            {
                if (myUnityApi.IsUnityType(typeElement))
                {
                    myContributor.AddUnityImplicitClassUsage(consumer, element,
                        "Unity scripting component", "Scripting component", kind);
                }
                else if (myUnityApi.IsUnityECSType(typeElement))
                {
                    myContributor.AddUnityImplicitClassUsage(consumer, element,
                        "Unity entity component system object", "Unity ECS", kind);
                }
                else if (myUnityApi.IsSerializableType(typeElement))
                {
                    myContributor.AddUnityImplicitClassUsage(consumer, element,
                        "Unity custom serializable type", "Unity serializable", kind);
                }

                return typeElement;
            }

            return null;
        }
    }
}