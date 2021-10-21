using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

#nullable enable

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
    public class LoadSceneAnalyzer : ProjectSettingsRelatedProblemAnalyzerBase<IInvocationExpression>
    {
        public LoadSceneAnalyzer(UnityApi unityApi, UnityProjectSettingsCache projectSettingsCache)
            : base(unityApi, projectSettingsCache)
        {
        }

        protected override void Analyze(IInvocationExpression invocationExpression, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            var argument = GetSceneNameArgument(invocationExpression);
            var literal = argument?.Value as ICSharpLiteralExpression;
            if (literal == null)
                return;

            if (invocationExpression.InvocationExpressionReference.IsSceneManagerSceneRelatedMethod())
            {
                var sceneName = GetScenePathFromArgument(literal);
                if (sceneName != null)
                {
                    // check build settings warnings
                    if (ProjectSettingsCache.IsScenePresentedAtEditorBuildSettings(sceneName,
                        out var ambiguousDefinition))
                    {
                        if (ProjectSettingsCache.IsSceneDisabledAtEditorBuildSettings(sceneName))
                        {
                            consumer.AddHighlighting(new LoadSceneDisabledSceneNameWarning(argument, sceneName));
                        }
                        else if (ambiguousDefinition)
                        {
                            consumer.AddHighlighting(
                                new LoadSceneAmbiguousSceneNameWarning(argument, sceneName));
                        }
                    }
                    else
                    {
                        if (ProjectSettingsCache.IsSceneExists(sceneName))
                        {
                            consumer.AddHighlighting(new LoadSceneUnknownSceneNameWarning(argument, sceneName));
                        } else
                        {
                            consumer.AddHighlighting(new LoadSceneUnexistingSceneNameWarning(argument));
                        }
                    }
                }
                else if (literal.ConstantValue.IsInteger())
                {
                    var value = (int) literal.ConstantValue.Value;
                    if (value >= ProjectSettingsCache.SceneCount)
                    {
                        consumer.AddHighlighting(new LoadSceneWrongIndexWarning(argument));
                    }
                }
            }

            if (invocationExpression.InvocationExpressionReference.IsEditorSceneManagerSceneRelatedMethod())
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

        private static ICSharpArgument? GetSceneNameArgument(IInvocationExpression invocationExpression)
        {
            return invocationExpression.ArgumentList.Arguments.FirstOrDefault
                (t => t.IsNamedArgument && t.ArgumentName?.Equals("sceneName") == true || !t.IsNamedArgument);
        }

        private static string? GetScenePathFromArgument(ICSharpLiteralExpression literalExpression)
        {
            // There are 3 ways to present scene name in unity
            // Consider scene : Assets/Scenes/myScene.unity
            // User could use "myScene", "Scenes/myScene" and "Assets/Scenes/myScene.unity" to load scene
            // Internally, we work only with first and second format (see UnityProjectSettingsCache)

            var constantValue = literalExpression.ConstantValue.Value as string;
            if (constantValue == null)
                return null;

            var sceneName = constantValue;
            if (sceneName.StartsWith("Assets/") && sceneName.EndsWith(UnityFileExtensions.SceneFileExtensionWithDot,
                StringComparison.InvariantCultureIgnoreCase))
            {
                sceneName = UnityProjectSettingsUtils.GetUnityScenePathRepresentation(sceneName);
            }

            return sceneName;
        }
    }
}