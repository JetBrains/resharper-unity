#nullable enable

using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new []
    {
        typeof(UnknownLayerWarning)
    })]
    public class LayerMaskAnalyzer : ProjectSettingsRelatedProblemAnalyzerBase<IInvocationExpression>
    {
        public LayerMaskAnalyzer(UnityApi unityApi, UnityProjectSettingsCache projectSettingsCache)
            : base(unityApi, projectSettingsCache)
        {
        }

        protected override void Analyze(IInvocationExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (element.IsLayerMaskGetMaskMethod() || element.IsLayerMaskNameToLayerMethod())
            {
                foreach (var argument in element.ArgumentList.Arguments)
                {
                    // TODO: Use conditional access when the monorepo build uses a more modern C# compiler
                    // Currently (as of 01/2023) the monorepo build for Unity uses C#9 compiler, which will complain
                    // that the out variable is uninitialised when we use conditional access
                    // See also https://youtrack.jetbrains.com/issue/RSRP-489147
                    var argumentValue = argument?.Value?.ConstantValue;
                    if (argumentValue != null && argumentValue.IsNotNullString(out var literal) &&
                        !ProjectSettingsCache.HasLayer(literal))
                    {
                        consumer.AddHighlighting(new UnknownLayerWarning(argument));
                    }
                }
            }
        }
    }
}
