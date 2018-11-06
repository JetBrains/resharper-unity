using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes
{
    [QuickFix]
    public class MoveNullComparisonQuickFix : AbstractMoveQuickFix
    {
        public MoveNullComparisonQuickFix(PerformanceNullComparisonHighlighting warning)
            : base(warning.Expression.GetContainingNode<IClassDeclaration>(), warning.Expression, warning.FieldName)
        {
        }
    }
}