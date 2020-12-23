using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage
{
    public abstract class CallGraphCommentMarksProvider : CallGraphRootMarksProviderBase
    {
        private readonly string myNameToFind;

        protected CallGraphCommentMarksProvider(string nameToFind, string name, ICallGraphPropagator propagator, bool isEnabled = true)
            : base(name, propagator, isEnabled)
        {
            myNameToFind = nameToFind;
        }

        public bool HasMarkComment(ITreeNode node, out bool isMarked)
        {
            var commentToFind = myNameToFind;
            
            return UnityCallGraphUtil.HasAnalysisComment(commentToFind, node, out isMarked);
        }

        private LocalList<IDeclaredElement> GetDeclaredElement([NotNull] ITreeNode node, bool isMarked)
        {
            var result = new LocalList<IDeclaredElement>();

            if (HasMarkComment(node, out var marked) && marked == isMarked)
            {
                var methodDeclaration = node as IMethodDeclaration;
                var declaredElement = methodDeclaration?.DeclaredElement;

                Assertion.AssertNotNull(declaredElement, "declaredElement != null");
                result.Add(declaredElement);
            }

            return result;
        }

        public override LocalList<IDeclaredElement> GetBanMarksFromNode(ITreeNode currentNode, IDeclaredElement containingFunction)
        {
            return GetDeclaredElement(currentNode, isMarked: false);
        }

        public override LocalList<IDeclaredElement> GetRootMarksFromNode(ITreeNode currentNode, IDeclaredElement containingFunction)
        {
            return GetDeclaredElement(currentNode, isMarked: true);
        }
    }
}