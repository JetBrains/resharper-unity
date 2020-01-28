using JetBrains.Application;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph
{
    [ShellComponent]
    public class StringBasedInvocationEdgeProvider : ICallGraphImplicitEdgeProvider
    {
        public LocalList<IDeclaredElement> ResolveImplicitlyInvokedDeclaredElements(ITreeNode treeNode)
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
                        var result =  new LocalList<IDeclaredElement>(1);
                        result.Add(implicitlyInvokeDeclaredElement);
                        return result;
                    }
                }
            }

            return new LocalList<IDeclaredElement>();
        }
    }
}