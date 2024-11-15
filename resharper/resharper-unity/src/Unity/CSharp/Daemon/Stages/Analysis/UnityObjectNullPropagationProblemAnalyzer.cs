#nullable enable
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[ElementProblemAnalyzer(Instantiation.DemandAnyThreadSafe, typeof(IConditionalAccessExpression), HighlightingTypes = [typeof(UnityObjectNullPropagationWarning)])]
public class UnityObjectNullPropagationProblemAnalyzer(UnityApi unityApi, UnityLifetimeChecksHelper helper) : UnityElementProblemAnalyzer<IConditionalAccessExpression>(unityApi)
{
    protected override void Analyze(IConditionalAccessExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (expression is { HasConditionalAccessSign: true, ConditionalQualifier: {} qualifier } && helper.CanBeDestroyed(qualifier))
            consumer.AddHighlighting(new UnityObjectNullPropagationWarning(expression.ConditionalAccessSign));
    }
}