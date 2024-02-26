#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[ElementProblemAnalyzer(typeof(IConditionalAccessExpression), HighlightingTypes = [typeof(UnityObjectNullPropagationWarning)])]
public class UnityObjectNullPropagationProblemAnalyzer(UnityApi unityApi, UnityLifetimeChecksHelper helper) : UnityElementProblemAnalyzer<IConditionalAccessExpression>(unityApi)
{
    public override bool ShouldRun(IFile file, ElementProblemAnalyzerData data) => helper.ForceLifetimeChecks.Value && base.ShouldRun(file, data);
    
    protected override void Analyze(IConditionalAccessExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (expression is { HasConditionalAccessSign: true, ConditionalQualifier: {} qualifier } && helper.CanBeDestroyed(qualifier))
            consumer.AddHighlighting(new UnityObjectNullPropagationWarning(expression));
    }
}