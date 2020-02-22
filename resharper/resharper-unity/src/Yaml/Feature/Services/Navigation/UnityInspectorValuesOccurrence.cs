using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityInspectorValuesOccurrence: UnityAssetOccurrence
    {
        public InspectorVariableUsage InspectorVariableUsage { get; }

        public UnityInspectorValuesOccurrence(IPsiSourceFile sourceFile, InspectorVariableUsage inspectorVariableUsage,
            IDeclaredElementPointer<IDeclaredElement> declaredElement, IHierarchyElement attachedElement)
            : base(sourceFile, declaredElement, attachedElement)
        {
            InspectorVariableUsage = inspectorVariableUsage;
        }

        public override string ToString()
        {
            using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
            {
                return $"{InspectorVariableUsage.Name} = {InspectorVariableUsage.Value.GetPresentation(GetSolution(), DeclaredElementPointer.FindDeclaredElement(), true)}";
            }
        }
    }
}