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
    internal partial class JsonNewArray
    {
        public IJsonNewValue AddArrayElementBefore(IJsonNewValue value, IJsonNewValue? anchor)
        {
            if (anchor == null)
            {
                var lastItem = Values.LastOrDefault();
                if (lastItem != null)
                    return AddArrayElementAfter(value, lastItem);

                // This is the only array element. Don't worry about whitespace
                using (WriteLockCookie.Create(parent.IsPhysical()))
                {
                    if (RBracket != null)
                        value = ModificationUtil.AddChildBefore(RBracket, value);
                    else if (LBracket != null)
                        value = ModificationUtil.AddChildAfter(LBracket, value);
                    value.AssertIsValid();
                    return value;
                }
            }

            Assertion.Assert(Values.Contains(anchor));

            // Copy the leading whitespace from the anchor, since we don't currently support formatting
            var whitespaceStart = anchor.GetPreviousNonWhitespaceToken()?.GetNextToken();
            var whitespaceEnd = anchor.GetPreviousToken();

            using (WriteLockCookie.Create(parent.IsPhysical()))
            {
                var comma = JsonNewTokenNodeTypes.COMMA.CreateLeafElement();
                LowLevelModificationUtil.AddChildBefore(anchor, comma);
                value = ModificationUtil.AddChildBefore(comma, value);

                if (whitespaceStart != null && whitespaceStart.IsWhitespaceToken() &&
                    whitespaceEnd != null && whitespaceEnd.IsWhitespaceToken())
                {
                    ModificationUtil.AddChildRangeBefore(anchor, new TreeRange(whitespaceStart, whitespaceEnd));
                }

                value.AssertIsValid();
                return value;
            }
        }

        public IJsonNewValue AddArrayElementAfter(IJsonNewValue value, IJsonNewValue? anchor)
        {
            if (anchor == null)
            {
                var firstItem = ValuesEnumerable.FirstOrDefault();
                if (firstItem != null)
                    return AddArrayElementBefore(value, firstItem);

                // This is the only array element. Don't worry about whitespace
                using (WriteLockCookie.Create(parent.IsPhysical()))
                {
                    if (RBracket != null)
                        ModificationUtil.AddChildBefore(RBracket, value);
                    else if (LBracket != null)
                        ModificationUtil.AddChildAfter(LBracket, value);
                    value.AssertIsValid();
                    return value;
                }
            }

            Assertion.Assert(Values.Contains(anchor));

            // Copy the leading whitespace from the anchor, since we don't currently support formatting
            var whitespaceStart = anchor.GetPreviousNonWhitespaceToken()?.GetNextToken();
            var whitespaceEnd = anchor.GetPreviousToken();

            using (WriteLockCookie.Create(parent.IsPhysical()))
            {
                var comma = JsonNewTokenNodeTypes.COMMA.CreateLeafElement();
                LowLevelModificationUtil.AddChildAfter(anchor, comma);
                value = ModificationUtil.AddChildAfter(comma, value);

                if (whitespaceStart != null && whitespaceStart.IsWhitespaceToken() &&
                    whitespaceEnd != null && whitespaceEnd.IsWhitespaceToken())
                {
                    ModificationUtil.AddChildRangeAfter(comma, new TreeRange(whitespaceStart, whitespaceEnd));
                }

                value.AssertIsValid();
                return value;
            }
        }

        public void RemoveArrayElement(IJsonNewValue argument)
        {
            using (WriteLockCookie.Create(IsPhysical()))
            {
                // Find corresponding separator (if any) and delete children
                ITreeNode startNode = argument;
                while (true)
                {
                    var prevStartNode = startNode.PrevSibling;
                    if (prevStartNode == null)
                        break;

                    if (prevStartNode.GetTokenType()?.IsWhitespace == false &&
                        prevStartNode.GetTokenType() != JsonNewTokenNodeTypes.COMMA)
                    {
                        break;
                    }

                    startNode = prevStartNode;
                    if (startNode.GetTokenType() == JsonNewTokenNodeTypes.COMMA)
                        break;
                }

                ITreeNode endNode = argument;
                while (true)
                {
                    var nextEndNode = endNode.NextSibling;
                    if (nextEndNode == null)
                        break;

                    if (nextEndNode.GetTokenType()?.IsWhitespace == false)
                        break;

                    endNode = nextEndNode;
                }

                // Don't eat the whitespace at the end of the array
                if (endNode.GetTokenType() != JsonNewTokenNodeTypes.COMMA)
                    endNode = argument;

                ModificationUtil.DeleteChildRange(startNode, endNode);
            }
        }
    }
}
