using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Resources.Shell;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Json.Psi.Tree.Impl
{
    internal partial class JsonNewObject
    {
        public IJsonNewMember AddMemberBefore(string key, IJsonNewValue value, IJsonNewMember? anchor)
        {
            if (anchor == null)
            {
                var lastItem = Members.LastOrDefault();
                if (lastItem != null)
                    return AddMemberAfter(key, value, lastItem);

                using (WriteLockCookie.Create(parent.IsPhysical()))
                {
                    var member = CreateMember(key, value);
                    if (RBrace != null)
                        ModificationUtil.AddChildBefore(RBrace, member);
                    else if (LBrace != null)
                        ModificationUtil.AddChildAfter(LBrace, member);
                    return member;
                }
            }

            Assertion.Assert(Members.Contains(anchor));
            using (WriteLockCookie.Create(parent.IsPhysical()))
            {
                var member = CreateMember(key, value);
                var comma = JsonNewTokenNodeTypes.COMMA.CreateLeafElement();
                LowLevelModificationUtil.AddChildBefore(anchor, comma);
                ModificationUtil.AddChildBefore(comma, member);
                return member;
            }
        }

        public IJsonNewMember AddMemberAfter(string key, IJsonNewValue value, IJsonNewMember? anchor)
        {
            if (anchor == null)
            {
                var firstItem = MembersEnumerable.FirstOrDefault();
                if (firstItem != null)
                    return AddMemberBefore(key, value, firstItem);

                using (WriteLockCookie.Create(parent.IsPhysical()))
                {
                    var member = CreateMember(key, value);
                    if (RBrace != null)
                        ModificationUtil.AddChildBefore(RBrace, member);
                    else if (LBrace != null)
                        ModificationUtil.AddChildAfter(LBrace, member);
                    return member;
                }
            }

            Assertion.Assert(Members.Contains(anchor));
            using (WriteLockCookie.Create(parent.IsPhysical()))
            {
                var member = CreateMember(key, value);
                var comma = JsonNewTokenNodeTypes.COMMA.CreateLeafElement();
                LowLevelModificationUtil.AddChildAfter(anchor, comma);
                ModificationUtil.AddChildAfter(comma, member);
                return member;
            }
        }

        private static IJsonNewMember CreateMember(string key, IJsonNewValue value)
        {
            var member = (JsonNewMember)ElementType.JSON_NEW_MEMBER.Create();
            member.AddChild(JsonNewTokenNodeTypes.DOUBLE_QUOTED_STRING.CreateLeafElement($"\"{key}\""));
            member.AddChild(JsonNewTokenNodeTypes.COLON.CreateLeafElement());
            member.AddChild(value);
            return member;
        }
    }
}