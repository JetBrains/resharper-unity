using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi.CSharp.Impl.CodeStyle.MemberReordering;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle.MemberReordering
{
    public class UnityEventFunction : ISortDescriptor, INodeConstraint, IComparer<ITreeNode>
    {
        private readonly UnityEventFunctionComparer myComparer = new UnityEventFunctionComparer();

        [UsedImplicitly]
        [DefaultValue(SortDirection.Ascending)]
        public SortDirection Direction { get; set; }

        public bool Matches(ITreeNode node)
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

        public IComparer<ITreeNode> GetComparer() => this;

        public int Compare(ITreeNode x, ITreeNode y)
        {
            var methodX = x as IMethodDeclaration;
            var methodY = y as IMethodDeclaration;
            return Sign * myComparer.Compare(methodX?.DeclaredName, methodY?.DeclaredName);
        }

        private int Sign => Direction == SortDirection.Ascending ? 1 : -1;
    }
}