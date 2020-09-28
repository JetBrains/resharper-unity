using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph
{
    public static class UnityCallGraphUtil
    {
        public static bool IsFunctionNode(ITreeNode node)
        {
            switch (node)
            {
                case IFunctionDeclaration _:
                case ICSharpClosure _:
                    return true;
                default:
                    return false;
            }
        }
        
        public static DaemonProcessKind GetProcessKindForGraph([NotNull] SolutionAnalysisService solutionAnalysisService)
        {
            return solutionAnalysisService.Configuration?.Completed?.Value == true
                ? DaemonProcessKind.GLOBAL_WARNINGS
                : DaemonProcessKind.VISIBLE_DOCUMENT;
        }

        public static bool IsSweaCompleted([NotNull] SolutionAnalysisService solutionAnalysisService)
        {
            return solutionAnalysisService.Configuration?.Enabled?.Value == true;
        }
    }
}