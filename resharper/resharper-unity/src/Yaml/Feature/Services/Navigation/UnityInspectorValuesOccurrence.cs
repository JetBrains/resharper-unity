using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetInspectorValues;
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
            return $"{InspectorVariableUsage.Name} = {InspectorVariableUsage.Value.GetPresentation(GetSolution(), DeclaredElementPointer.FindDeclaredElement())}";
        }
    }
}