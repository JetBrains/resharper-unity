namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    public interface IUnityHierarchyElement
    {
        FileID Id { get;}
        FileID CorrespondingSourceObject { get;}
        FileID PrefabInstance { get;}
        bool IsStripped { get;}
    }
}