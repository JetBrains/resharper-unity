using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetMethods;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityMethodsOccurrence : UnityAssetOccurrence
    {
        public readonly AssetMethodData MethodData;

        public UnityMethodsOccurrence(IPsiSourceFile sourceFile, IDeclaredElementPointer<IDeclaredElement> declaredElement,
            IHierarchyElement attachedElement, LocalReference attachedElementLocation, AssetMethodData methodData)
            : base(sourceFile, declaredElement, attachedElement, attachedElementLocation)
        {
            MethodData = methodData;
        }


        public override string ToString()
        {
            return $"m_MethodName: {MethodData.MethodName}";
        }
    }
}