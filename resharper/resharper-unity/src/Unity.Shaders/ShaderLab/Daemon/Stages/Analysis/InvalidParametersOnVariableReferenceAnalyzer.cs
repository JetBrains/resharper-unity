using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Errors;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvalidVariableReferenceParameters), HighlightingTypes = new[] { typeof(ShaderLabInvalidVariableReferenceParametersWarning)})]
    public class InvalidParametersOnVariableReferenceAnalyzer : ShaderLabElementProblemAnalyzer<IInvalidVariableReferenceParameters>
    {
        protected override void Analyze(IInvalidVariableReferenceParameters element, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            // I'd like this to be an error, but the shader compiler silently ignores it
            // and fails to create a valid variable reference
            consumer.AddHighlighting(new ShaderLabInvalidVariableReferenceParametersWarning(element, 
                element.GetHighlightingRange()));
        }
    }
}