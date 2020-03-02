using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

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
            if (currentNode is IStructDeclaration structDeclaration
                && structDeclaration.GetAttribute(KnownTypes.BurstCompile) != null &&
                myAPI.IsDescendantOf(KnownTypes.Job, structDeclaration.DeclaredElement))
            {
                foreach (var methodDeclaration in structDeclaration.MethodDeclarations)
                {
                    if (methodDeclaration.DeclaredName == "Execute" &&
                        methodDeclaration.Params.ParameterDeclarations.Count == 0)
                    {
                        res.Add(methodDeclaration.DeclaredElement);
                        break;
                    }
                }
            }


            return new LocalList<IDeclaredElement>(res);
        }
    }
}