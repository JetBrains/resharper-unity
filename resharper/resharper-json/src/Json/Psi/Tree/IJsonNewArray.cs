namespace JetBrains.ReSharper.Plugins.Json.Psi.Tree
{
    public partial interface IJsonNewArray
    {
        void RemoveArrayElement(IJsonNewValue argument);
    }
}