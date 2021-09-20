using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new []
    {
        typeof(UnknownLayerWarning)
    })]
    public class LayerMaskAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private readonly UnityProjectSettingsCache myProjectSettingsCache;

        public LayerMaskAnalyzer(UnityApi unityApi, UnityProjectSettingsCache projectSettingsCache)
            : base(unityApi)
        {
            myProjectSettingsCache = projectSettingsCache;
        }

        protected override void Analyze(IInvocationExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!myProjectSettingsCache.IsAvailable())
                return;

            if (element.IsLayerMaskGetMaskMethod() || element.IsLayerMaskNameToLayerMethod())
            {
                foreach (var argument in element.ArgumentList.Arguments)
                {
                    var literal = (argument?.Value as ICSharpLiteralExpression)?.ConstantValue.Value as string;
                    if (literal == null)
                        return;

                    if (myProjectSettingsCache != null && !myProjectSettingsCache.HasLayer(literal))
                        consumer.AddHighlighting(new UnknownLayerWarning(argument));
                }
            }
        }
    }
}