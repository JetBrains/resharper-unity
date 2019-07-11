namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Swa
{
    public interface IUnityHierarchyElement
    {
        FileID Id { get;}
        FileID CorrespondingSourceObject { get;}
        FileID PrefabInstance { get;}
        bool IsStripped { get;}
    }
}