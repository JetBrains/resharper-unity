#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[ElementProblemAnalyzer(typeof(IIsExpression), HighlightingTypes = [typeof(UnityObjectLifetimeCheckBypassedByIsOperatorWarning)])]
public class UnityObjectLifetimeCheckBypassedByIsOperatorProblemAnalyzer(UnityApi unityApi, UnityCSharpAnalysisConfig analysisConfig) : UnityElementProblemAnalyzer<IIsExpression>(unityApi)
{
    public override bool ShouldRun(IFile file, ElementProblemAnalyzerData data) => analysisConfig.ForceLifetimeChecks.Value && base.ShouldRun(file, data);
    
    protected override void Analyze(IIsExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (expression.Operand is { } operand && UnityTypeUtils.IsUnityObject(operand.Type()))
            consumer.AddHighlighting(new UnityObjectLifetimeCheckBypassedByIsOperatorWarning(expression));
    }
}