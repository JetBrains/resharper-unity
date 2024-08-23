#nullable enable
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[ElementProblemAnalyzer(Instantiation.DemandAnyThreadUnsafe, typeof(IAssignmentExpression), HighlightingTypes = [typeof(UnityObjectNullCoalescingWarning)])]
public class UnityObjectNullCoalescingAssignmentProblemAnalyzer(UnityApi unityApi, UnityLifetimeChecksHelper helper) : UnityElementProblemAnalyzer<IAssignmentExpression>(unityApi)
{
    protected override void Analyze(IAssignmentExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (expression is { OperatorSign: {} operatorSign, AssignmentType: AssignmentType.DOUBLE_QUEST_EQ, Source: not null, Dest: { } dest } && helper.CanBeDestroyed(dest))
            consumer.AddHighlighting(new UnityObjectNullCoalescingWarning(operatorSign));
    }
}