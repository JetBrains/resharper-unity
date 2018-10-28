using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes
{
    [QuickFix]
    public class MoveCameraMainToStartQuickFix : AbstractMoveQuickFix
    {
        public MoveCameraMainToStartQuickFix(PerformanceCriticalCodeCameraMainHighlighting warning) : 
            base(warning.ReferenceExpression.GetContainingNode<IClassDeclaration>(), warning.ReferenceExpression)
        {
        }
    }
}