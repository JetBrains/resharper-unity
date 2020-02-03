namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    public interface IUnityHierarchyElement
    {
        AssetDocumentReference Id { get;}
        AssetDocumentReference CorrespondingSourceObject { get;}
        AssetDocumentReference PrefabInstance { get;}
        bool IsStripped { get;}
    }
}