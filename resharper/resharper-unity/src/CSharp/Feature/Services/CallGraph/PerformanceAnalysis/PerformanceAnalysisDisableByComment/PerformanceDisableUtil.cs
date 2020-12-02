using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.PerformanceAnalysisDisableByComment
{
    public static class PerformanceDisableUtil
    {
        public const string MESSAGE = "Disable performance analysis for method";
        public const string COMMENT = "// " + ReSharperControlConstruct.DisablePrefix + " " +
                                      UnityCallGraphUtil.PerformanceExpensiveComment;

        [ContractAnnotation("null => false")]
        public static bool IsAvailable([CanBeNull] IMethodDeclaration methodDeclaration)
        {
            if (methodDeclaration == null)
                return false;

            methodDeclaration.GetPsiServices().Locks.AssertReadAccessAllowed();

            var declaredElement = methodDeclaration.DeclaredElement;

            return declaredElement != null && methodDeclaration.IsValid() &&
                   !PerformanceCriticalCodeStageUtil.IsPerformanceCriticalRootMethod(methodDeclaration);
        }
    }
}