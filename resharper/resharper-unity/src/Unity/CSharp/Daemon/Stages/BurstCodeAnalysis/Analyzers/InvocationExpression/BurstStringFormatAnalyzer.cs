using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.BurstCodeAnalysisUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.InvocationExpression
{
    [SolutionComponent]
    public class BurstStringFormatAnalyzer : IBurstProblemSubAnalyzer<IInvocationExpression>
    {
        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(IInvocationExpression invocationExpression,
            IHighlightingConsumer consumer)
        {
            var invokedMethod = invocationExpression.Reference.Resolve().DeclaredElement as IMethod;

            if (invokedMethod == null)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_STOP;

            if (!IsStringFormat(invokedMethod))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            var argumentList = invocationExpression.ArgumentList.Arguments;

            if (argumentList.Count == 0)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_STOP;

            var firstArgument = argumentList[0];

            switch (firstArgument.Expression)
            {
                case ICSharpLiteralExpression cSharpLiteralExpression when cSharpLiteralExpression.Literal.GetTokenType().IsStringLiteral:
                case IReferenceExpression referenceExpression when referenceExpression.IsConstantValue() && referenceExpression.Type().IsString():
                case IInterpolatedStringExpression:
                    break;
                default:
                    consumer?.AddHighlighting(new BurstStringFormatInvalidFormatWarning(firstArgument.Expression));
                    return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
            }

            var burstProblemSubAnalyzerStatus = BurstProblemSubAnalyzerStatus.NO_WARNING_STOP;
            for (var index = 1; index < argumentList.Count; index++)
            {
                var argument = argumentList[index];
                var argumentExpression = argument.Expression;
                var type = argumentExpression?.Type() as IDeclaredType;
                if (IsBurstPermittedType(type) && !type.IsString())
                    continue;

                consumer?.AddHighlighting(new BurstStringFormatInvalidArgumentWarning(argumentExpression, type.GetClrName().ShortName, index - 1));
                burstProblemSubAnalyzerStatus = BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
            }

            return burstProblemSubAnalyzerStatus;
        }

        public int Priority => 2000;
    }
}