using JetBrains.ReSharper.Plugins.Json.Util;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Tree.Impl
{
    internal partial class JsonNewMember
    {
        public string Key => StringLiteralUtil.GetDoubleQuotedStringValue(KeyToken);
    }
}