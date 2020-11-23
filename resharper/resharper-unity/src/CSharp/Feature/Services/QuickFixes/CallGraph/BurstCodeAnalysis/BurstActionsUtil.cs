using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    public static class BurstActionsUtil
    {
        [ContractAnnotation("null => false")]
        public static bool IsAvailable([CanBeNull] IMethodDeclaration methodDeclaration)
        {
            if (methodDeclaration == null)
                return false;

            methodDeclaration.GetPsiServices().Locks.AssertReadAccessAllowed();

            var declaredElement = methodDeclaration.DeclaredElement;

            return declaredElement != null && methodDeclaration.IsValid() &&
                   !BurstCodeAnalysisUtil.IsBurstProhibitedFunction(declaredElement);
        }
    }
}