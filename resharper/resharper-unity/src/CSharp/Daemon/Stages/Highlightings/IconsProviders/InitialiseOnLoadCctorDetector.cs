using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class InitialiseOnLoadCctorDetector : IUnityDeclarationHiglightingProvider
    {
        private readonly UnityApi myUnityApi;
        private readonly UnityHighlightingContributor myContributor;

        public InitialiseOnLoadCctorDetector(UnityApi unityApi,UnityHighlightingContributor contributor)
        {
            myUnityApi = unityApi;
            myContributor = contributor;
        }

        public IDeclaredElement Analyze(IDeclaration node, IHighlightingConsumer consumer, DaemonProcessKind kind)
        {
            if (!(node is IConstructorDeclaration element))
                return null;
            
            if (!element.IsStatic)
                return null;

            var containingType = element.GetContainingTypeDeclaration()?.DeclaredElement;
            if (containingType != null &&
                containingType.HasAttributeInstance(KnownTypes.InitializeOnLoadAttribute, false))
            {
                myContributor.AddInitializeOnLoadMethod(consumer, element,"Called when Unity first launches the editor, the player, or recompiles scripts", 
                    "Unity implicit usage", kind);

                return containingType;
            }

            return null;
        }
    }
}