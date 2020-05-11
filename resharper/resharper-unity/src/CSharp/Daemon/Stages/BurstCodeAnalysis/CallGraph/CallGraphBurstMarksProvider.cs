using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph
{
    [SolutionComponent]
    public class CallGraphBurstMarksProvider : CallGraphRootMarksProviderBase
    {
        public CallGraphBurstMarksProvider(ISolution solution)
            : base(nameof(CallGraphBurstMarksProvider),
                new CallGraphOutcomingPropagator(solution, nameof(CallGraphBurstMarksProvider)))
        {
        }

        public override LocalList<IDeclaredElement> GetRootMarksFromNode(ITreeNode currentNode,
            IDeclaredElement containingFunction)
        {
            var res = new HashSet<IDeclaredElement>();
            switch (currentNode)
            {
                case IMethodDeclaration methodDeclaration
                    when methodDeclaration.DeclaredElement is IMethod method &&
                         method.ShortName == "Execute" &&
                         method.Parameters.Count == 0 && method.TypeParameters.Count == 0 &&
                         method.GetContainingType() is IStruct @struct &&
                         @struct.HasAttributeInstance(KnownTypes.BurstCompileAttribute, AttributesSource.Self) &&
                         UnityApi.IsDescendantOf(KnownTypes.Job, @struct):
                {
                    res.Add(method);
                    break;
                }
                case IInvocationExpression invocationExpression:
                {
                    if (!(CallGraphUtil.GetCallee(invocationExpression) is IMethod method))
                        break;
                    var containingType = method.GetContainingType();
                    if (containingType == null)
                        break;
                    if (method.Parameters.Count == 1 &&
                        method.TypeParameters.Count == 1 &&
                        method.ShortName == "CompileFunctionPointer" &&
                        containingType.GetClrName().Equals(KnownTypes.BurstCompiler))
                    {
                        var argumentList = invocationExpression.ArgumentList.Arguments;
                        if (argumentList.Count != 1)
                            break;
                        var argument = argumentList[0].Value;
                        if (argument == null)
                            break;
                        var possibleDeclaredElements = CallGraphUtil.ExtractCallGraphDeclaredElements(argument);
                        foreach (var declaredElement in possibleDeclaredElements)
                        {
                            if (declaredElement != null)
                                res.Add(declaredElement);
                        }
                    }

                    break;
                }
            }

            return new LocalList<IDeclaredElement>(res);
        }

        public override bool IsRootMark(IDeclaredElement declaredElement, IDeclaration elementNode)
        {
            return false;
        }

        public override LocalList<IDeclaredElement> GetBanMarksFromNode(ITreeNode currentNode,
            IDeclaredElement containingFunction)
        {
            return new LocalList<IDeclaredElement>();
        }

        public override bool IsBannedMark(IDeclaredElement declaredElement, IDeclaration elementNode)
        {
            var methodDeclaration = elementNode as IMethodDeclaration;
            var method = methodDeclaration?.DeclaredElement;
            if (method == null)
                return false;
            Assertion.Assert(ReferenceEquals(declaredElement, method), "ReferenceEquals(declaredElement, method)");
            return method.HasAttributeInstance(KnownTypes.BurstDiscardAttribute, AttributesSource.Self);
        }
    }
}