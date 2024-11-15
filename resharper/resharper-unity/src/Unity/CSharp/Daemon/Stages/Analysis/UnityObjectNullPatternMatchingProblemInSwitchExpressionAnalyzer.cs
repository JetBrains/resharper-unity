#nullable enable
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[ElementProblemAnalyzer(Instantiation.DemandAnyThreadSafe, typeof(ISwitchExpression), HighlightingTypes = [typeof(UnityObjectNullPatternMatchingWarning)])]
public class UnityObjectNullPatternMatchingProblemInSwitchExpressionAnalyzer(UnityApi unityApi, UnityLifetimeChecksHelper helper) : UnityElementProblemAnalyzer<ISwitchExpression>(unityApi)
{
    protected override void Analyze(ISwitchExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (expression.GoverningExpression is { } objectExpression && helper.CanBeDestroyed(objectExpression))
        {
            foreach (var arm in expression.Arms)
            {
                if (arm.Pattern is {} armPattern)
                    helper.AddNullPatternMatchingWarnings(armPattern, consumer);
            }
        }   
    }
}
