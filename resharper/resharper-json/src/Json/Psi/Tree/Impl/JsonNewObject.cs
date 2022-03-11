#nullable enable

using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;

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

                // This is the only member. Don't worry about whitespace
                using (WriteLockCookie.Create(parent.IsPhysical()))
                {
                    var member = CreateMember(key, value);
                    if (RBrace != null)
                        member = ModificationUtil.AddChildBefore(RBrace, member);
                    if (LBrace != null)
                        member = ModificationUtil.AddChildAfter(LBrace, member);
                    member.AssertIsValid();
                    return member;
                }
            }

            // Copy the leading whitespace from the anchor, since we don't currently support formatting
            var whitespaceStart = anchor.GetPreviousNonWhitespaceToken()?.GetNextToken();
            var whitespaceEnd = anchor.GetPreviousToken();

            Assertion.Assert(Members.Contains(anchor));
            using (WriteLockCookie.Create(parent.IsPhysical()))
            {
                var member = CreateMember(key, value);
                var comma = JsonNewTokenNodeTypes.COMMA.CreateLeafElement();
                LowLevelModificationUtil.AddChildBefore(anchor, comma);
                member = ModificationUtil.AddChildBefore(comma, member);

                if (whitespaceStart != null && whitespaceStart.IsWhitespaceToken() &&
                    whitespaceEnd != null && whitespaceEnd.IsWhitespaceToken())
                {
                    ModificationUtil.AddChildRangeBefore(anchor, new TreeRange(whitespaceStart, whitespaceEnd));
                }

                member.AssertIsValid();
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

                // This is the only member. Don't worry about whitespace
                using (WriteLockCookie.Create(parent.IsPhysical()))
                {
                    var member = CreateMember(key, value);
                    if (RBrace != null)
                        member = ModificationUtil.AddChildBefore(RBrace, member);
                    else if (LBrace != null)
                        member = ModificationUtil.AddChildAfter(LBrace, member);
                    member.AssertIsValid();
                    return member;
                }
            }

            Assertion.Assert(Members.Contains(anchor));

            // Copy the leading whitespace from the anchor, since we don't currently support formatting
            var whitespaceStart = anchor.GetPreviousNonWhitespaceToken()?.GetNextToken();
            var whitespaceEnd = anchor.GetPreviousToken();

            using (WriteLockCookie.Create(parent.IsPhysical()))
            {
                var member = CreateMember(key, value);
                var comma = JsonNewTokenNodeTypes.COMMA.CreateLeafElement();
                LowLevelModificationUtil.AddChildAfter(anchor, comma);
                member = ModificationUtil.AddChildAfter(comma, member);

                if (whitespaceStart != null && whitespaceStart.IsWhitespaceToken() &&
                    whitespaceEnd != null && whitespaceEnd.IsWhitespaceToken())
                {
                    ModificationUtil.AddChildRangeAfter(comma, new TreeRange(whitespaceStart, whitespaceEnd));
                }

                member.AssertIsValid();
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
