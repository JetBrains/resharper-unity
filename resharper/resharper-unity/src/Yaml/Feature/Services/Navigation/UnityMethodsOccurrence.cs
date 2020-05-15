using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityMethodsOccurrence : UnityAssetOccurrence
    {
        public bool IsPrefabModification { get; }
        public readonly AssetMethodUsages MethodUsages;

        public UnityMethodsOccurrence(IPsiSourceFile sourceFile, IDeclaredElementPointer<IDeclaredElement> declaredElement,
            LocalReference owningElementLocation, AssetMethodUsages methodUsages, bool isPrefabModification)
            : base(sourceFile, declaredElement, owningElementLocation)
        {
            IsPrefabModification = isPrefabModification;
            MethodUsages = methodUsages;
        }


        public override string ToString()
        {
            return $"m_MethodName: {MethodUsages.MethodName}";
        }

        public override IconId GetIcon()
        {
            if (IsPrefabModification)
                return UnityFileTypeThemedIcons.FileUnityPrefab.Id;

            return base.GetIcon();
        }
    }
}