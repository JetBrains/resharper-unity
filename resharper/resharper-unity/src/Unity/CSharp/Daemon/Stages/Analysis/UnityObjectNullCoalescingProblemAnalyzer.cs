#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[ElementProblemAnalyzer(typeof(INullCoalescingExpression), HighlightingTypes = new[] { typeof(UnityObjectNullCoalescingWarning) })]
public class UnityObjectNullCoalescingProblemAnalyzer(UnityApi unityApi, UnityCSharpAnalysisConfig analysisConfig) : UnityElementProblemAnalyzer<INullCoalescingExpression>(unityApi)
{
    public override bool ShouldRun(IFile file, ElementProblemAnalyzerData data) => analysisConfig.ForceLifetimeChecks.Value && base.ShouldRun(file, data);
    
    protected override void Analyze(INullCoalescingExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (expression is { RightOperand: not null, LeftOperand: {} left } && UnityTypeUtils.IsUnityObject(left.Type()))
            consumer.AddHighlighting(new UnityObjectNullCoalescingWarning(expression));
    }
}