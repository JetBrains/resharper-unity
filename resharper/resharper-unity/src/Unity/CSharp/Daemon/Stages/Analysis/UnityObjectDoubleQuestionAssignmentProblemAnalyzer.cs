#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[ElementProblemAnalyzer(typeof(IAssignmentExpression), HighlightingTypes = [typeof(UnityObjectDoubleQuestionAssignmentWarning)])]
public class UnityObjectDoubleQuestionAssignmentProblemAnalyzer(UnityApi unityApi, UnityLifetimeChecksHelper helper) : UnityElementProblemAnalyzer<IAssignmentExpression>(unityApi)
{
    public override bool ShouldRun(IFile file, ElementProblemAnalyzerData data) => helper.ForceLifetimeChecks.Value && base.ShouldRun(file, data);

    protected override void Analyze(IAssignmentExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (expression is { AssignmentType: AssignmentType.DOUBLE_QUEST_EQ, Source: not null, Dest: { } dest } && helper.CanBeDestroyed(dest))
            consumer.AddHighlighting(new UnityObjectDoubleQuestionAssignmentWarning(expression));
    }
}