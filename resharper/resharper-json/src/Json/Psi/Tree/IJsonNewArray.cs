#nullable enable

namespace JetBrains.ReSharper.Plugins.Json.Psi.Tree
{
    public partial interface IJsonNewArray
    {
        void AddArrayElementBefore(IJsonNewValue value, IJsonNewValue? anchor);
        void AddArrayElementAfter(IJsonNewValue value, IJsonNewValue? anchor);
        void RemoveArrayElement(IJsonNewValue argument);
    }
}