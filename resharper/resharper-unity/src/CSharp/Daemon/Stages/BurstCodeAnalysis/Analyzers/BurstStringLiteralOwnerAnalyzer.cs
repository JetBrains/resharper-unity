using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstStringLiteralOwnerAnalyzer : BurstProblemAnalyzerBase<IStringLiteralOwner>
    {
        public static bool CheckAndAnalyze(ITreeNode startNode, IHighlighting highlighting,
            IHighlightingConsumer consumer)
        {
            var firstNode = startNode;
            do
            {
                var parent = startNode.Parent;

                switch (parent)
                {
                    case IExpressionInitializer expressionInitializer:
                    {
                        if (ReferenceEquals(expressionInitializer.Value, firstNode))
                        {
                            do
                            {
                                parent = parent.Parent;
                            } while (parent != null && !(parent is IInitializerOwnerDeclaration));

                            if (parent == null)
                                return true;

                            var initializerOwnerDeclaration = (IInitializerOwnerDeclaration) parent;

                            if (ReferenceEquals(initializerOwnerDeclaration.Initializer, expressionInitializer))
                            {
                                var typeOwner = initializerOwnerDeclaration.DeclaredElement as ITypeOwner;
                                var type = typeOwner?.Type;

                                if (BurstCodeAnalysisUtil.IsFixedString(type))
                                    return false;
                            }
                        }

                        consumer?.AddHighlighting(highlighting);
                        return true;
                    }
                    case ICSharpArgument cSharpArgument:
                    {
                        var invocationInfo = cSharpArgument.Invocation;

                        if (invocationInfo is IInvocationExpression invocationExpression)
                        {
                            var callee = invocationExpression.Reference.Resolve().DeclaredElement as IMethod;

                            if (BurstCodeAnalysisUtil.IsBurstPossibleArgumentString(cSharpArgument)
                                && callee != null
                                && (BurstCodeAnalysisUtil.IsDebugLog(callee) ||
                                    BurstCodeAnalysisUtil.IsStringFormat(callee)))
                                return false;
                        }

                        consumer?.AddHighlighting(highlighting);
                        return true;
                    }
                    case IAssignmentExpression assignmentExpression
                        when assignmentExpression.Dest == startNode ||
                             BurstCodeAnalysisUtil.IsFixedString(assignmentExpression.Dest.Type()) &&
                             assignmentExpression.Source.Type().IsString():
                        return false;
                    case IAssignmentExpression _:
                    case ITypeMemberDeclaration _:
                        consumer?.AddHighlighting(highlighting);
                        return true;
                    default:
                        startNode = parent;
                        break;
                }
            } while (startNode != null);

            consumer?.AddHighlighting(highlighting);
            return true;
        }

        protected override bool CheckAndAnalyze(IStringLiteralOwner stringLiteralOwner, IHighlightingConsumer consumer)
        {
            bool isString;

            if (stringLiteralOwner is ICSharpLiteralExpression cSharpLiteralExpression)
                isString = cSharpLiteralExpression.Literal.GetTokenType().IsStringLiteral;
            else
                isString = true;

            return isString && CheckAndAnalyze(stringLiteralOwner,
                new BurstManagedStringWarning(stringLiteralOwner), consumer);
        }
    }
}