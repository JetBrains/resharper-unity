using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes
{
    [QuickFix]
    public class MoveCostlyInvocationtQuickFix : AbstractMoveQuickFix
    {
        private readonly bool myWarningIsMoveToStartAvailable;

        public MoveCostlyInvocationtQuickFix(PerformanceCriticalCodeInvocationHighlighting warning) :
            base(warning.InvocationExpression?.GetContainingNode<IClassDeclaration>(), warning.InvocationExpression)
        {
            myWarningIsMoveToStartAvailable = warning.IsMoveToStartAvailable;
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return myWarningIsMoveToStartAvailable && base.IsAvailable(cache);
        }
    }
}