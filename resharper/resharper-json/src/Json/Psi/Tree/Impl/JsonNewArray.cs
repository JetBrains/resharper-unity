using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Tree.Impl
{
    internal partial class JsonNewArray
    {
        public void RemoveArrayElement([NotNull] IJsonNewValue argument)
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