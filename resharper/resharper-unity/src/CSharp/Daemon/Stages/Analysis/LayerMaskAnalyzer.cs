using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Yaml.Settings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new []
    {
        typeof(UnknownLayerWarning)
    })]
    public class LayerMaskAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private readonly AssetSerializationMode myAssetSerializationMode;
        private readonly YamlSupport myUnityYamlSupport;

        public LayerMaskAnalyzer([NotNull] UnityApi unityApi, AssetSerializationMode assetSerializationMode,
            YamlSupport unityYamlSupport)
            : base(unityApi)
        {
            myAssetSerializationMode = assetSerializationMode;
            myUnityYamlSupport = unityYamlSupport;
        }

        protected override void Analyze(IInvocationExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!myAssetSerializationMode.IsForceText)
                return;

            if (!myUnityYamlSupport.IsParsingEnabled.Value)
                return;

            if (element.IsLayerMaskGetMaskMethod() || element.IsLayerMaskNameToLayerMethod())
            {
                foreach (var argument in element.ArgumentList.Arguments)
                {
                    var literal = (argument?.Value as ICSharpLiteralExpression)?.ConstantValue.Value as string;
                    if (literal == null)
                        return;

                    var cache = element.GetSolution().TryGetComponent<UnityProjectSettingsCache>();
                    if (cache != null && !cache.HasLayer(literal))
                        consumer.AddHighlighting(new UnknownLayerWarning(argument));
                }
            }
        }
    }
}