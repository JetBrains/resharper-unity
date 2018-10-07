using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util.Literals;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes =
        new[] {typeof(PreferGenericMethodOverloadWarning)})]
    public class PreferGenericMethodOverloadAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        public PreferGenericMethodOverloadAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IInvocationExpression expression, ElementProblemAnalyzerData data,
                                        IHighlightingConsumer consumer)
        {
            // MATT: Is this check really necessary?
            if (expression.RPar == null) return;

            if (!(expression.InvokedExpression is IReferenceExpression) || expression.TypeArguments.Count != 0) return;

            var literalExpressionArgument = expression.Arguments.SingleItem?.Value as ILiteralExpression;
            if (literalExpressionArgument == null || !literalExpressionArgument.Literal.IsAnyStringLiteral()) return;

            var methodName = GetMethodName(expression.Reference);
            if (methodName != "GetComponent" && methodName != "AddComponent" && methodName != "CreateInstance") return;

            var references = literalExpressionArgument.GetReferences<UnityObjectTypeOrNamespaceReference>();

            // Don't add anything unless ALL references resolve properly
            foreach (var reference in references)
            {
                var resolveInfo = reference.Resolve();
                if (!resolveInfo.Info.ResolveErrorType.IsAcceptable)
                    return;
            }

            foreach (var reference in references)
            {
                var resolveInfo = reference.Resolve();
                if (resolveInfo.DeclaredElement is ITypeElement typeElement)
                {
                    consumer.AddHighlighting(new PreferGenericMethodOverloadWarning(expression, methodName,
                        literalExpressionArgument, typeElement));
                }
            }


            // MATT: What is this preprocessor thing?
//            if (suitableTypes.Length > 0  && !expression.ContainsPreprocessorDirectives())
//            {
//                consumer.AddHighlighting(new PreferGenericMethodOverloadWarning(expression, methodName, argument, suitableTypes));
//            }
        }

        private static string GetMethodName([CanBeNull] IReference reference)
        {
            var info = reference?.Resolve();
            return info?.ResolveErrorType == ResolveErrorType.OK ? info.DeclaredElement?.ShortName : null;
        }
    }
}