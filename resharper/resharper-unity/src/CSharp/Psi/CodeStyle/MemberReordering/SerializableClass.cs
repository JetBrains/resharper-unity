using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl.CodeStyle.MemberReordering;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle.MemberReordering
{
    public class SerializableClass : INodeConstraint
    {
        public bool Matches(ITreeNode node, INodeMatchingContext ctx)
        {
            if (!(node is IDeclaration declaration)) return false;
            var unityApi = node.GetSolution().GetComponent<UnityApi>();
            return unityApi.IsUnityType(declaration.DeclaredElement as ITypeElement)
                   || unityApi.IsSerializableType(declaration.DeclaredElement as ITypeElement);
        }

        public int? Compare(INodeConstraint other)
        {
            if (other == null || other == Unconstrained.Instance)
                return -1;

            return null;
        }
    }
}