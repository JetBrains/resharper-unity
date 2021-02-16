using JetBrains.Application;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph
{
    [ShellComponent]
    public class StringBasedInvocationEdgeProvider : ICallGraphEdgeProvider
    {
        public void FindEdges(ITreeNode treeNode, IDeclaredElement caller, ICallGraphEdgeConsumer consumer)
        {
            if (treeNode is IInvocationExpression invocationExpression)
            {
                var name = invocationExpression.Reference?.Resolve().DeclaredElement?.ShortName;

                if (name != null && (name.Equals("Invoke") || name.Equals("InvokeRepeating")))
                {
                    var implicitlyInvokeDeclaredElement = invocationExpression.Arguments.FirstOrDefault()?.Value
                        ?.GetReferences<UnityEventFunctionReference>().FirstOrDefault()?.Resolve().DeclaredElement;
                    if (implicitlyInvokeDeclaredElement != null)
                    {
                        consumer.AddEdge(caller, implicitlyInvokeDeclaredElement);
                    }
                }
            }
        }
    }
}