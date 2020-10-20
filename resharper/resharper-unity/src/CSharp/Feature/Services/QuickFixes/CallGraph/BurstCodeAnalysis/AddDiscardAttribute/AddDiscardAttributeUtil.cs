using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis.AddDiscardAttribute
{
    public static class AddDiscardAttributeUtil
    {
        public const string DiscardActionMessage = "Add `BurstDiscard` attribute";

        [ContractAnnotation("null => false")]
        public static bool IsAvailable([CanBeNull] IMethodDeclaration methodDeclaration)
        {
            if (methodDeclaration == null)
                return false;
            
            var declaredElement = methodDeclaration.DeclaredElement;

            return declaredElement != null && methodDeclaration.IsValid() &&
                   !BurstCodeAnalysisUtil.IsBurstProhibitedFunction(declaredElement);
        }
    }
}