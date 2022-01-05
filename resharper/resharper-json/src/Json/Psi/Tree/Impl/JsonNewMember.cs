using JetBrains.ReSharper.Plugins.Unity.JsonNew.Util;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree.Impl
{
    internal partial class JsonNewMember
    {
        public string Key => StringLiteralUtil.GetDoubleQuotedStringValue(KeyToken);
    }
}