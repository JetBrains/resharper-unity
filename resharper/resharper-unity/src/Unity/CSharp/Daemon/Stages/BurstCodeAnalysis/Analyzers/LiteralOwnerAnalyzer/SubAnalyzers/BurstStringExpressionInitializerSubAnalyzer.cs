using JetBrains.Application.Parts;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.LiteralOwnerAnalyzer.SubAnalyzers
{
    
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public sealed class BurstStringExpressionInitializerSubAnalyzer : BurstStringSubAnalyzerBase<IExpressionInitializer>
    {
        protected override IExpressionInitializer Navigate(ICSharpExpression expression)
        {
            return ExpressionInitializerNavigator.GetByValue(expression);
        }

        protected override bool AnalyzeNode(IExpressionInitializer navigated, ICSharpExpression from)
        {
            Assertion.Assert(ReferenceEquals(navigated.Value, from), "navigated should be from");

            var initializerOwnerDeclaration = navigated.GetContainingNode<IInitializerOwnerDeclaration>();
            var initializer = initializerOwnerDeclaration?.Initializer;

            if (ReferenceEquals(initializer, navigated))
            {
                var typeOwner = initializerOwnerDeclaration.DeclaredElement as ITypeOwner;
                var type = typeOwner?.Type;

                if (BurstCodeAnalysisUtil.IsFixedString(type))
                    return false;
            }


            return true;
        }
    }
}