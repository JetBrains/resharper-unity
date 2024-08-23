#nullable enable
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[ElementProblemAnalyzer(Instantiation.DemandAnyThreadUnsafe, typeof(INullCoalescingExpression), HighlightingTypes = [typeof(UnityObjectNullCoalescingWarning)])]
public class UnityObjectNullCoalescingProblemAnalyzer(UnityApi unityApi, UnityLifetimeChecksHelper helper) : UnityElementProblemAnalyzer<INullCoalescingExpression>(unityApi)
{
    protected override void Analyze(INullCoalescingExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (expression is { RightOperand: not null, LeftOperand: {} left } && helper.CanBeDestroyed(left))
            consumer.AddHighlighting(new UnityObjectNullCoalescingWarning(expression.OperatorSign));
    }
}