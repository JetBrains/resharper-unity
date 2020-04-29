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
        public readonly AssetMethodData MethodData;

        public UnityMethodsOccurrence(IPsiSourceFile sourceFile, IDeclaredElementPointer<IDeclaredElement> declaredElement,
            LocalReference attachedElementLocation, AssetMethodData methodData, bool isPrefabModification)
            : base(sourceFile, declaredElement, attachedElementLocation)
        {
            IsPrefabModification = isPrefabModification;
            MethodData = methodData;
        }


        public override string ToString()
        {
            return $"m_MethodName: {MethodData.MethodName}";
        }

        public override IconId GetIcon()
        {
            if (IsPrefabModification)
                return UnityFileTypeThemedIcons.FileUnityPrefab.Id;

            return base.GetIcon();
        }
    }
}