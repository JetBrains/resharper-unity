using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util.Literals;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression))]
    public class LoadSceneAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        public LoadSceneAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IInvocationExpression invocationExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (IsSceneManagerLoadScene(invocationExpression.InvocationExpressionReference))
            {
                var firstArgument = invocationExpression.ArgumentList.Arguments.FirstOrDefault();
                if (firstArgument == null) 
                    return;

                if (firstArgument.Value is ICSharpLiteralExpression literalExpression)
                {
                    if (literalExpression.Literal.IsAnyStringLiteral())
                    {
                        var cache = invocationExpression.GetSolution().TryGetComponent<UnityProjectSettingsCache>();
                        if (cache == null)
                            return;
                        if (!cache.IsScenePresentedAtEditorBuildSettings(literalExpression.ConstantValue.Value as string,
                            out var ambiguousDefention))
                        {
                            consumer.AddHighlighting(new LoadSceneUnknownSceneNameWarning(firstArgument));
                        }

                        if (ambiguousDefention)
                        {
                            consumer.AddHighlighting(new LoadSceneAmbiguousSceneNameWarning(firstArgument));
                        }
                    } else if (literalExpression.ConstantValue.IsInteger())
                    {
                        var value = (int) literalExpression.ConstantValue.Value;
                        var cache = invocationExpression.GetSolution().TryGetComponent<UnityProjectSettingsCache>();
                        if (cache == null)
                            return;

                        if (value >= cache.SceneCount)
                        {
                            consumer.AddHighlighting(new LoadSceneWrongIndexWarning(firstArgument));
                        }
                    }
                }
            }
        }

        public static bool IsSceneManagerLoadScene(IInvocationExpressionReference reference)
        {
            var result = reference.Resolve();
            if (IsSceneManagerLoadScene(result.DeclaredElement as IMethod))
                return true;
            
            foreach (var candidate in result.Result.Candidates)
            {
                if (IsSceneManagerLoadScene(candidate as IMethod))
                    return true;
            }

            return false;
        }

        private static bool IsSceneManagerLoadScene(IMethod method)
        {
            if (method != null && method.ShortName.Equals("LoadScene") &&
                method.GetContainingType()?.GetClrName().Equals(KnownTypes.SceneManager) == true)
            {
                return true;
            }

            return false;
        }
    }
}