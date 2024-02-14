#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[ElementProblemAnalyzer(typeof(IAssignmentExpression), HighlightingTypes = new[] { typeof(UnityObjectDoubleQuestionAssignmentWarning) })]
public class UnityObjectDoubleQuestionAssignmentProblemAnalyzer(UnityApi unityApi) : UnityElementProblemAnalyzer<IAssignmentExpression>(unityApi)
{
    protected override void Analyze(IAssignmentExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (expression is { AssignmentType: AssignmentType.DOUBLE_QUEST_EQ, Source: not null, Dest: { } dest } && UnityTypeUtils.IsUnityObject(dest.Type()))
            consumer.AddHighlighting(new UnityObjectDoubleQuestionAssignmentWarning(expression));
    }
}