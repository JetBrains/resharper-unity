using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstStringLiteralOwnerAnalyzer : BurstProblemAnalyzerBase<IStringLiteralOwner>
    {
        public static bool CheckAndAnalyze(ITreeNode node, IHighlighting highlighting, IHighlightingConsumer consumer)
        {
            var firstNode = node;
            do
            {
                var parent = node.Parent;

                if (parent is IExpressionInitializer expressionInitializer)
                {
                    if (ReferenceEquals(expressionInitializer.Value, firstNode))
                    {
                        do
                        {
                            node = node.Parent;
                            parent = node.Parent;
                        } while (!(parent is IInitializerOwnerDeclaration));

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

                if (parent is ICSharpArgument cSharpArgument)
                {
                    var invocationInfo = cSharpArgument.Invocation;
                    if (invocationInfo is IInvocationExpression info)
                    {
                        var callee = CallGraphUtil.GetCallee(info) as IMethod;

                        if (BurstCodeAnalysisUtil.IsBurstPermittedString(cSharpArgument.GetExpressionType().ToIType())
                            && callee != null
                            && (BurstCodeAnalysisUtil.IsDebugLog(callee) ||
                                BurstCodeAnalysisUtil.IsStringFormat(callee)))
                            return false;
                    }

                    consumer?.AddHighlighting(highlighting);
                    return true;
                }

                if (parent is IAssignmentExpression assignmentExpression)
                {
                    if (assignmentExpression.Dest == node)
                        return false;

                    if (BurstCodeAnalysisUtil.IsFixedString(assignmentExpression.Dest.Type()) &&
                        assignmentExpression.Source.Type().IsString())
                        return false;

                    consumer?.AddHighlighting(highlighting);
                    return true;
                }

                if (parent is ITypeMemberDeclaration)
                {
                    consumer?.AddHighlighting(highlighting);
                    return true;
                }

                node = parent;
            } while (node != null);

            consumer?.AddHighlighting(highlighting);
            return true;
        }
        protected override bool CheckAndAnalyze(IStringLiteralOwner stringLiteralOwner, IHighlightingConsumer consumer)
        {
            var isString = stringLiteralOwner.Type().IsString();
            if (!isString)
                return false;

            return CheckAndAnalyze(stringLiteralOwner, new BC1349Error(stringLiteralOwner.GetDocumentRange()), consumer);
        }
    }
}