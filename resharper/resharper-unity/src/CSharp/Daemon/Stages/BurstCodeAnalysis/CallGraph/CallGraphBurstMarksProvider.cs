using System.Collections.Generic;
using System.Linq;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
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
        private static readonly IClrTypeName TestAttribute = new ClrTypeName("TestAttribute");
        public CallGraphBurstMarksProvider(ISolution solution)
            : base(nameof(CallGraphBurstMarksProvider),
                new CallGraphOutcomingPropagator(solution, nameof(CallGraphBurstMarksProvider)))
        {
        }

        public override LocalList<IDeclaredElement> GetRootMarksFromNode(ITreeNode currentNode,
            IDeclaredElement containingFunction)
        {
            var result = new HashSet<IDeclaredElement>();
            switch (currentNode)
            {
                #if JET_MODE_ASSERT
                case IMethodDeclaration methodDeclaration when methodDeclaration.DeclaredElement is IMethod method && 
                    method.HasAttributeInstance(TestAttribute, AttributesSource.Self):
                {
                    result.Add(method);
                    break;
                }
                #endif
                case IStructDeclaration structDeclaration
                    when structDeclaration.DeclaredElement is IStruct @struct &&
                         @struct.HasAttributeInstance(KnownTypes.BurstCompileAttribute, AttributesSource.Self):
                {
                    var visited = new HashSet<ITypeElement>();
                    var interfaces = new LocalList<ITypeElement>();
                    var todo = new Stack<ITypeElement>();
                    visited.Add(@struct);
                    todo.Push(@struct);

                    while (!todo.IsEmpty())
                    {
                        var current = todo.Pop();
                        foreach (var typeElement in current.GetSuperTypeElements())
                        {
                            var @interface = typeElement as IInterface;
                            if (@interface == null)
                                continue;
                            if (visited.Add(@interface))
                            {
                                todo.Push(@interface);
                                if (@interface.HasAttributeInstance(KnownTypes.JobProducer, AttributesSource.Self))
                                    interfaces.Add(@interface);
                            }
                        }
                    }

                    foreach (var @interface in interfaces)
                    {
                        var interfaceMethods = @interface.Methods;
                        var structMethods = @struct.Methods;
                        var overridenMethods = structMethods
                            .Where(m => interfaceMethods.Any(m.OverridesOrImplements)).ToList();
                        foreach (var overridenMethod in overridenMethods)
                            result.Add(overridenMethod);
                    }

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
                                result.Add(declaredElement);
                        }
                    }

                    break;
                }
            }

            return new LocalList<IDeclaredElement>(result);
        }

        public override LocalList<IDeclaredElement> GetBanMarksFromNode(ITreeNode currentNode,
            IDeclaredElement containingFunction)
        {
            var result = new LocalList<IDeclaredElement>();
            if (containingFunction == null)
                return result;
            var methodDeclaration = currentNode as IMethodDeclaration;
            var method = methodDeclaration?.DeclaredElement;
            if (method == null)
                return result;
            if (method.HasAttributeInstance(KnownTypes.BurstDiscardAttribute, AttributesSource.Self))
                result.Add(containingFunction);
            return result;
        }
    }
}