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
            if (currentNode is IMethodDeclaration methodDeclaration &&
                methodDeclaration.GetContainingTypeDeclaration() is IStructDeclaration structDeclaration &&
                structDeclaration.GetAttribute(KnownTypes.BurstCompile) != null &&
                myAPI.IsDescendantOf(KnownTypes.Job, structDeclaration.DeclaredElement))
            {
                var declaredElement = methodDeclaration.DeclaredElement;
                if (declaredElement != null &&
                    declaredElement.ShortName == "Execute" &&
                    declaredElement.Parameters.Count == 0 &&
                    declaredElement.TypeParameters.Count == 0)
                {
                    res.Add(declaredElement);
                }
            }


            return new LocalList<IDeclaredElement>(res);
        }
    }
}