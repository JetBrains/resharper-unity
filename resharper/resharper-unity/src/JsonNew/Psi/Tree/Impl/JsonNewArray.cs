using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree.Impl
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
                    if (prevStartNode.GetTokenType() != JsonNewTokenNodeTypes.WHITE_SPACE &&
                        prevStartNode.GetTokenType() != JsonNewTokenNodeTypes.COMMA)
                        break;
                    
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
                    if (nextEndNode.GetTokenType() != JsonNewTokenNodeTypes.WHITE_SPACE)
                        break;

                    endNode = nextEndNode;
                }

                ModificationUtil.DeleteChildRange(startNode, endNode);
            }
        }
    }
}