using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.LiteralOwnerAnalyzer.SubAnalyzers
{
    [SolutionComponent]
    public class BurstStringAssignmentExpressionSubAnalyzer : BurstStringSubAnalyzerBase<IAssignmentExpression>
    {
        protected override IAssignmentExpression Navigate(ICSharpExpression expression)
        {
            return AssignmentExpressionNavigator.GetBySource(expression);
        }

        protected override bool AnalyzeNode(IAssignmentExpression navigated, ICSharpExpression from)
        {
            var lhs = navigated.Dest;
            var rhs = navigated.Source;

            if (BurstCodeAnalysisUtil.IsFixedString(lhs.Type()) && rhs.Type().IsString())
                return false;

            return true;
        }
    }
}