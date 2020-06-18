using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp.Impl.CodeStyle.MemberReordering;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle.MemberReordering
{
    public class EventFunction : INodeConstraint
    {
        public bool Matches(ITreeNode node, INodeMatchingContext ctx)
        {
            if (node is IMethodDeclaration methodDeclaration)
            {
                var unityApi = node.GetSolution().GetComponent<UnityApi>();
                return unityApi.IsEventFunction(methodDeclaration.DeclaredElement);
            }

            return false;
        }

        public int? Compare(INodeConstraint other)
        {
            if (other == null || other == Unconstrained.Instance)
                return -1;

            return null;
        }
    }
}