#nullable enable

using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Impl.CodeStyle.MemberReordering;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle.MemberReordering
{
    public class AutoPropertyWithSerializedBackingField : INodeConstraint
    {
        public bool Matches(ITreeNode node, INodeMatchingContext ctx)
        {
            if (node is not IPropertyDeclaration propertyDeclaration) return false;

            var unityApi = node.GetSolution().GetComponent<UnityApi>();
            return unityApi.IsSerialisedAutoProperty(propertyDeclaration.DeclaredElement, false) ==
                   SerializedFieldStatus.SerializedField;
        }

        public int? Compare(INodeConstraint? other) => other == null || other == Unconstrained.Instance ? -1 : null;
    }
}
