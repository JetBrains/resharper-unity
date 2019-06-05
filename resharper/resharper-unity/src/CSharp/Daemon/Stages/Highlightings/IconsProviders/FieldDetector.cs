using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{

    [SolutionComponent]
    public class FieldDetector : IUnityDeclarationHiglightingProvider
    {
        private readonly UnityApi myUnityApi;
        private readonly UnityHighlightingContributor myImplicitUsageHighlightingContributor;

        public FieldDetector(UnityApi unityApi, UnityHighlightingContributor implicitUsageHighlightingContributor)
        {
            myUnityApi = unityApi;
            myImplicitUsageHighlightingContributor = implicitUsageHighlightingContributor;
        }

        public IDeclaredElement Analyze(IDeclaration element, IHighlightingConsumer consumer, DaemonProcessKind kind)
        {
            if (!(element is IFieldDeclaration field))
                return null;

            var declaredElement = field.DeclaredElement;
            if (declaredElement == null)
                return null;

            bool isSerializedField = myUnityApi.IsSerialisedField(declaredElement);
            if (isSerializedField && (
                    myUnityApi.IsDescendantOfMonoBehaviour(declaredElement.GetContainingType()) ||
                    myUnityApi.IsDescendantOfScriptableObject(declaredElement.GetContainingType())
                )
                || myUnityApi.IsInjectedField(declaredElement))
            {
                myImplicitUsageHighlightingContributor.AddUnityImplicitFieldUsage(consumer, field,
                    "This field is initialised by Unity", "Property", kind);

                return declaredElement;
            }

            if (isSerializedField && declaredElement.GetAttributeInstances(false)
                    .All(t => !t.GetClrName().Equals(KnownTypes.SerializeField)))
            {
                myImplicitUsageHighlightingContributor.AddUnityImplicitFieldUsage(consumer, field,
                    "This field is serialized by Unity", "Serializable", kind);
                return declaredElement;
            }
            
            return null;
        }
    }
}