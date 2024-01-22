#nullable enable

using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Impl.CodeStyle.MemberReordering;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle.MemberReordering
{
    public class SerializedField : INodeConstraint
    {
        public bool Matches(ITreeNode node, INodeMatchingContext ctx)
        {
            if (node is IMultipleFieldDeclaration multipleFieldDeclaration)
            {
                var unityApi = node.GetSolution().GetComponent<UnityApi>();
                foreach (var multipleDeclarationMember in multipleFieldDeclaration.DeclaratorsEnumerable)
                {
                    if (multipleDeclarationMember is IFieldDeclaration field &&
                        unityApi.IsSerialisedField(field.DeclaredElement).HasFlag(SerializedFieldStatus.SerializedField))
                    {
                        return true;
                    }
                }
            }

            if (node is IFieldDeclaration fieldDeclaration)
            {
                var unityApi = node.GetSolution().GetComponent<UnityApi>();
                return unityApi.IsSerialisedField(fieldDeclaration.DeclaredElement).HasFlag(SerializedFieldStatus.SerializedField);
            }

            return false;
        }

        public int? Compare(INodeConstraint? other) => other == null || other == Unconstrained.Instance ? -1 : null;
    }
}