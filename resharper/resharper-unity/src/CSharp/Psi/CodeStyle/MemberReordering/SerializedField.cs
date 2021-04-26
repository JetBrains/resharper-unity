using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp.Impl.CodeStyle.MemberReordering;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle.MemberReordering
{
    public class SerializedField : INodeConstraint
    {
        public bool Matches(ITreeNode node, INodeMatchingContext ctx)
        {
            var unityApi = node.GetSolution().GetComponent<UnityApi>();
            if (node is IMultipleFieldDeclaration multipleFieldDeclaration)
            {
                foreach (var multipleDeclarationMember in multipleFieldDeclaration.DeclaratorsEnumerable)
                {
                    if (multipleDeclarationMember is IFieldDeclaration field)
                    {
                        if (unityApi.IsSerialisedField(field.DeclaredElement))
                            return true;
                    }
                }
            }

            if (node is IFieldDeclaration fieldDeclaration)
                return unityApi.IsSerialisedField(fieldDeclaration.DeclaredElement);

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