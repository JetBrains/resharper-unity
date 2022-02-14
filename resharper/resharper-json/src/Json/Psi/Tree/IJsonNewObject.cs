#nullable enable

namespace JetBrains.ReSharper.Plugins.Json.Psi.Tree
{
    public partial interface IJsonNewObject
    {
        IJsonNewMember AddMemberBefore(string key, IJsonNewValue value, IJsonNewMember? anchor);
        IJsonNewMember AddMemberAfter(string key, IJsonNewValue value, IJsonNewMember? anchor);
    }
}