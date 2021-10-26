using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.LiteralOwnerAnalyzer.SubAnalyzers
{
    
    [SolutionComponent]
    public class BurstStringCSharpArgumentSubAnalyzer : BurstStringSubAnalyzerBase<ICSharpArgument>
    {
        protected override ICSharpArgument Navigate(ICSharpExpression expression)
        {
            return CSharpArgumentNavigator.GetByValue(expression);
        }

        protected override bool AnalyzeNode(ICSharpArgument navigated, ICSharpExpression from)
        {
            var invocationExpression = InvocationExpressionNavigator.GetByArgument(navigated);

            if (invocationExpression == null) return true;
            
            var callee = invocationExpression.Reference.Resolve().DeclaredElement as IMethod;
            var parameterType = navigated.MatchingParameter?.Type;

            return !BurstCodeAnalysisUtil.IsBurstPossibleArgumentString(navigated) || 
                   callee == null || 
                   !BurstCodeAnalysisUtil.IsDebugLog(callee) && !BurstCodeAnalysisUtil.IsStringFormat(callee) && !BurstCodeAnalysisUtil.IsFixedString(parameterType);
        }
    }
}