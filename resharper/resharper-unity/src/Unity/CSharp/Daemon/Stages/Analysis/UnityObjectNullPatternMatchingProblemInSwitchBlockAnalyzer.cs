#nullable enable
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[ElementProblemAnalyzer(Instantiation.DemandAnyThreadSafe, typeof(ISwitchStatement), HighlightingTypes = [typeof(UnityObjectNullPatternMatchingWarning)])]
public class UnityObjectNullPatternMatchingProblemInSwitchBlockAnalyzer(UnityApi unityApi, UnityLifetimeChecksHelper helper) : UnityElementProblemAnalyzer<ISwitchStatement>(unityApi)
{
    protected override void Analyze(ISwitchStatement expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (expression.GoverningExpression is { } objectExpression && helper.CanBeDestroyed(objectExpression))
        {
            foreach (var section in expression.SectionsEnumerable)
            {
                foreach (var caseLabel in section.CaseLabelsEnumerable)
                {
                    if (caseLabel.Pattern is {} armPattern)
                        helper.AddNullPatternMatchingWarnings(armPattern, consumer);
                }
            }
        }   
    }
}