using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Special;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph
{
    [SolutionComponent]
    public class CallGraphBurstMarksProvider : CallGraphRootMarksProviderBase
    {
        private readonly UnityApi myAPI;

        public CallGraphBurstMarksProvider(ISolution solution, UnityApi api)
            : base(nameof(CallGraphBurstMarksProvider),
                new CallGraphOutcomingPropagator(solution, nameof(CallGraphBurstMarksProvider)))
        {
            myAPI = api;
        }

        public override LocalList<IDeclaredElement> GetMarkedFunctionsFrom(ITreeNode currentNode,
            IDeclaredElement containingFunction)
        {
            var res = new HashSet<IDeclaredElement>();
            switch (currentNode)
            {
                case IMethodDeclaration methodDeclaration
                    when methodDeclaration.GetContainingTypeDeclaration() is IStructDeclaration structDeclaration &&
                         structDeclaration.GetAttribute(KnownTypes.BurstCompileAttribute) != null &&
                         myAPI.IsDescendantOf(KnownTypes.Job, structDeclaration.DeclaredElement):
                {
                    var declaredElement = methodDeclaration.DeclaredElement;
                    if (declaredElement != null &&
                        declaredElement.ShortName == "Execute" &&
                        declaredElement.Parameters.Count == 0 &&
                        declaredElement.TypeParameters.Count == 0)
                    {
                        res.Add(declaredElement);
                    }

                    break;
                }
                case IInvocationExpression invocationExpression:
                {
                    if (!(CallGraphUtil.GetCallee(invocationExpression) is IMethod function))
                        break;
                    var containingType = function.GetContainingType();
                    if (containingType == null)
                        break;
                    if (containingType.GetClrName().Equals(KnownTypes.BurstCompiler) &&
                        function.Parameters.Count == 1 && 
                        function.TypeParameters.Count == 1 &&
                        function.ShortName == "CompileFunctionPointer")
                    {
                        var argumentList = invocationExpression.ArgumentList.Arguments;
                        if (argumentList.Count != 1)
                            break;
                        var argument = argumentList[0].Value;
                        if (argument == null)
                            break;
                        var possibleDeclaredElements = CallGraphUtil.ExtractDeclaredElement(argument);
                        foreach (var declaredElement in possibleDeclaredElements)
                            if (declaredElement != null)
                            {
                                res.Add(declaredElement);
                            }
                    }

                    break;
                }
            }


            return new LocalList<IDeclaredElement>(res);
        }
    }
}