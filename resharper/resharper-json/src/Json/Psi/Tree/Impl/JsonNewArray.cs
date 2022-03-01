using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Json.Psi.Tree.Impl
{
    internal partial class JsonNewArray
    {
        public void AddArrayElementBefore(IJsonNewValue value, IJsonNewValue? anchor)
        {
            if (anchor == null)
            {
                var lastItem = Values.LastOrDefault();
                if (lastItem != null)
                {
                    AddArrayElementAfter(value, lastItem);
                    return;
                }

                using (WriteLockCookie.Create(parent.IsPhysical()))
                {
                    if (RBracket != null)
                        ModificationUtil.AddChildBefore(RBracket, value);
                    else if (LBracket != null)
                        ModificationUtil.AddChildAfter(LBracket, value);
                }
            }
            else
            {
                Assertion.Assert(Values.Contains(anchor));
                using (WriteLockCookie.Create(parent.IsPhysical()))
                {
                    var comma = JsonNewTokenNodeTypes.COMMA.CreateLeafElement();
                    LowLevelModificationUtil.AddChildBefore(anchor, comma);
                    ModificationUtil.AddChildBefore(comma, value);
                }
            }
        }

        public void AddArrayElementAfter(IJsonNewValue value, IJsonNewValue? anchor)
        {
            if (anchor == null)
            {
                var firstItem = ValuesEnumerable.FirstOrDefault();
                if (firstItem != null)
                {
                    AddArrayElementBefore(value, firstItem);
                    return;
                }

                using (WriteLockCookie.Create(parent.IsPhysical()))
                {
                    if (RBracket != null)
                        ModificationUtil.AddChildBefore(RBracket, value);
                    else if (LBracket != null)
                        ModificationUtil.AddChildAfter(LBracket, value);
                }
            }
            else
            {
                Assertion.Assert(Values.Contains(anchor));
                using (WriteLockCookie.Create(parent.IsPhysical()))
                {
                    var comma = JsonNewTokenNodeTypes.COMMA.CreateLeafElement();
                    LowLevelModificationUtil.AddChildAfter(anchor, comma);
                    ModificationUtil.AddChildAfter(comma, value);
                }
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