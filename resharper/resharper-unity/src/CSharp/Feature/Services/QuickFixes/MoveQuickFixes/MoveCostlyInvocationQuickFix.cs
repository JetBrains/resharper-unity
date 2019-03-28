using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes
{
    [QuickFix]
    public class MoveCostlyInvocationQuickFix : AbstractMoveQuickFix
    {
        public MoveCostlyInvocationQuickFix(PerformanceInvocationHighlighting warning) :
            base(warning.InvocationExpression?.GetContainingNode<IClassDeclaration>(), warning.InvocationExpression)
        {
        }
    }
}