#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[ElementProblemAnalyzer(typeof(IIsExpression), HighlightingTypes = [typeof(UnityObjectNullPatternMatchingWarning)])]
public class UnityObjectNullPatternMatchingProblemAnalyzer(UnityApi unityApi, UnityLifetimeChecksHelper helper) : UnityElementProblemAnalyzer<IIsExpression>(unityApi)
{
    protected override void Analyze(IIsExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (expression.Operand is { } operand && helper.CanBeDestroyed(operand) && helper.IsLifetimeBypassPattern(expression.Pattern))
            consumer.AddHighlighting(new UnityObjectNullPatternMatchingWarning(expression.OperatorSign));
    }
}