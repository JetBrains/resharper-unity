using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using static JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityProjectSettingsUtils;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new []
    {
        typeof(LoadSceneDisabledSceneNameWarning),
        typeof(LoadSceneUnknownSceneNameWarning),
        typeof(LoadSceneUnexistingSceneNameWarning),
        typeof(LoadSceneAmbiguousSceneNameWarning),
        typeof(LoadSceneWrongIndexWarning)
    })]
    public class LoadSceneAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        public LoadSceneAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IInvocationExpression invocationExpression, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            var argument = GetSceneNameArgument(invocationExpression);

            var literal = argument?.Value as ICSharpLiteralExpression;
            if (literal == null)
                return;
            
            if (IsSceneManagerSceneRelatedMethod(invocationExpression.InvocationExpressionReference))
            {
                var cache = invocationExpression.GetSolution().TryGetComponent<UnityProjectSettingsCache>();
                if (cache == null)
                    return;
                
                var sceneName = GetScenePathFromArgument(literal);
                if (sceneName != null)
                {
                    // check build settings warnings
                    if (!cache.IsScenePresentedAtEditorBuildSettings(sceneName,
                        out var ambiguousDefinition))
                    {
                        if (cache.IsSceneDisabledAtEditorBuildSettings(sceneName))
                        {
                            consumer.AddHighlighting(new LoadSceneDisabledSceneNameWarning(argument, sceneName));
                        }
                        else if (cache.IsSceneExists(sceneName))
                        {
                            consumer.AddHighlighting(
                                new LoadSceneUnknownSceneNameWarning(argument, sceneName));
                        } else
                        {
                            consumer.AddHighlighting(new LoadSceneUnexistingSceneNameWarning(argument));
                        }
                    }

                    if (ambiguousDefinition)
                    {
                        consumer.AddHighlighting(
                            new LoadSceneAmbiguousSceneNameWarning(argument, sceneName));
                    } 
                } else if (literal.ConstantValue.IsInteger())
                {
                    var value = (int) literal.ConstantValue.Value;
                    if (value >= cache.SceneCount)
                    {
                        consumer.AddHighlighting(new LoadSceneWrongIndexWarning(argument));
                    }
                }
            }


            if (IsEditorSceneManagerSceneRelatedMethod(invocationExpression.InvocationExpressionReference))
            {
                var cache = invocationExpression.GetSolution().TryGetComponent<UnityProjectSettingsCache>();
                if (cache == null)
                    return;
                
                var sceneName = GetScenePathFromArgument(literal);
                if (sceneName != null)
                {
                    if (!cache.IsSceneExists(sceneName))
                    {
                        consumer.AddHighlighting(new LoadSceneUnexistingSceneNameWarning(argument));
                    } 
                }
            }
        }
    }
}