using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Yaml.Settings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using static JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityProjectSettingsUtils;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new []
    {
        typeof(UnknownInputAxesWarning)
    })]
    public class InputManagerAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private readonly AssetSerializationMode myAssetSerializationMode;
        private readonly YamlSupport myUnityYamlSupport;

        public InputManagerAnalyzer([NotNull] UnityApi unityApi, AssetSerializationMode assetSerializationMode,
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
            
            if (IsInputAxisMethod(element) || IsInputButtonMethod(element))
            {
                var argument = element.ArgumentList.Arguments.FirstOrDefault();
                if (argument == null)
                    return;

                var literal = (argument.Value as ICSharpLiteralExpression)?.ConstantValue.Value as string;
                if (literal == null)
                    return;
                
                var cache = element.GetSolution().TryGetComponent<UnityProjectSettingsCache>();
                if (cache != null && !cache.HasInput(literal)) 
                    consumer.AddHighlighting(new UnknownInputAxesWarning(argument));
            }
        }
    }
}