using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes
{
    [QuickFix]
    public class MoveNullComparisonQuickFix : AbstractMoveQuickFix
    {
        public MoveNullComparisonQuickFix(UnityPerformanceNullComparisonWarning warning)
            : base(warning.Expression.GetContainingNode<IClassDeclaration>(), warning.Expression, warning.FieldName)
        {
        }
    }
}