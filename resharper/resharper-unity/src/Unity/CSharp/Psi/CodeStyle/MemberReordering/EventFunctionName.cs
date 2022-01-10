using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api.Utils;
using JetBrains.ReSharper.Psi.CSharp.Impl.CodeStyle.MemberReordering;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle.MemberReordering
{
    // The default patterns can have constraints and sort descriptors implemented on the same class. Extensions can't
    // due to the way Rider's options page schemas allow extension elements. Having a separate class is possibly better
    // in this case, as it makes more sense to sort by "EventFunctionName" than "EventFunction"
    public class EventFunctionName : ISortDescriptor, IComparer<ITreeNode>
    {
        private readonly UnityEventFunctionComparer myComparer = new UnityEventFunctionComparer();

        [UsedImplicitly]
        [DefaultValue(SortDirection.Ascending)]
        public SortDirection Direction { get; set; }

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