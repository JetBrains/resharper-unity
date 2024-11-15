#nullable enable
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[ElementProblemAnalyzer(Instantiation.DemandAnyThreadSafe, typeof(ISubpattern), HighlightingTypes = [typeof(UnityObjectNullPatternMatchingWarning)])]
public class UnityObjectNullPatternMatchingProblemInSubpatternAnalyzer(UnityApi unityApi, UnityLifetimeChecksHelper helper) : UnityElementProblemAnalyzer<ISubpattern>(unityApi)
{
    protected override void Analyze(ISubpattern expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (expression.AccessExpression is { } operand && helper.CanBeDestroyed(operand))
            helper.AddNullPatternMatchingWarnings(expression.Pattern, consumer);
    }
}