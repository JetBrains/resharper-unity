#nullable enable

namespace JetBrains.ReSharper.Plugins.Json.Psi.Tree
{
    public partial interface IJsonNewArray
    {
        IJsonNewValue AddArrayElementBefore(IJsonNewValue value, IJsonNewValue? anchor);
        IJsonNewValue AddArrayElementAfter(IJsonNewValue value, IJsonNewValue? anchor);
        void RemoveArrayElement(IJsonNewValue argument);
    }
}
