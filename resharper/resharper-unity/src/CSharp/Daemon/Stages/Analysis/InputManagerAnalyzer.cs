using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new []
    {
        typeof(UnknownInputAxesWarning)
    })]
    public class InputManagerAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly AssetSerializationMode myAssetSerializationMode;
        private readonly UnityProjectSettingsCache myProjectSettingsCache;

        public InputManagerAnalyzer([NotNull] UnityApi unityApi,
                                    AssetIndexingSupport assetIndexingSupport,
                                    AssetSerializationMode assetSerializationMode,
                                    UnityProjectSettingsCache unityProjectSettingsCache)
            : base(unityApi)
        {
            myAssetIndexingSupport = assetIndexingSupport;
            myAssetSerializationMode = assetSerializationMode;
            myProjectSettingsCache = unityProjectSettingsCache;
        }

        protected override void Analyze(IInvocationExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!myAssetSerializationMode.IsForceText)
                return;

            if (!myAssetIndexingSupport.IsEnabled.Value)
                return;

            if (element.IsInputAxisMethod() || element.IsInputButtonMethod())
            {
                var argument = element.ArgumentList.Arguments.FirstOrDefault();
                var literal = (argument?.Value as ICSharpLiteralExpression)?.ConstantValue.Value as string;
                if (literal == null)
                    return;

                if (myProjectSettingsCache != null && !myProjectSettingsCache.HasInput(literal))
                    consumer.AddHighlighting(new UnknownInputAxesWarning(argument));
            }
        }
    }
}