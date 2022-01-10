using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes
{
    [QuickFix]
    public class MoveCameraMainQuickFix : AbstractMoveQuickFix
    {
        public MoveCameraMainQuickFix(UnityPerformanceCameraMainWarning warning) : 
            base(warning.ReferenceExpression.GetContainingNode<IClassDeclaration>(), warning.ReferenceExpression)
        {
        }
    }
}