using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

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
        private readonly UnityProjectSettingsCache myProjectSettingsCache;

        public LoadSceneAnalyzer(UnityApi unityApi, UnityProjectSettingsCache projectSettingsCache)
            : base(unityApi)
        {
            myProjectSettingsCache = projectSettingsCache;
        }

        protected override void Analyze(IInvocationExpression invocationExpression, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            if (!myProjectSettingsCache.IsAvailable())
                return;

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
                    if (myProjectSettingsCache.IsScenePresentedAtEditorBuildSettings(sceneName,
                        out var ambiguousDefinition))
                    {
                        if (myProjectSettingsCache.IsSceneDisabledAtEditorBuildSettings(sceneName))
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
                        if (myProjectSettingsCache.IsSceneExists(sceneName))
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
                    if (value >= myProjectSettingsCache.SceneCount)
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

        private static ICSharpArgument GetSceneNameArgument(IInvocationExpression invocationExpression)
        {
            return invocationExpression.ArgumentList.Arguments.FirstOrDefault
                (t => t.IsNamedArgument && t.ArgumentName?.Equals("sceneName") == true || !t.IsNamedArgument);
        }

        private static string GetScenePathFromArgument(ICSharpLiteralExpression literalExpression)
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