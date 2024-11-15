#nullable enable

using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(Instantiation.DemandAnyThreadSafe, typeof(IInvocationExpression),
        HighlightingTypes = new[] {typeof(UnknownAnimatorStateNameWarning)})]
    public class PlayAnimatorStateAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly AssetSerializationMode myAssetSerializationMode;

        public PlayAnimatorStateAnalyzer(UnityApi unityApi,
                                         AssetIndexingSupport assetIndexingSupport,
                                         AssetSerializationMode assetSerializationMode)
            : base(unityApi)
        {
            myAssetIndexingSupport = assetIndexingSupport;
            myAssetSerializationMode = assetSerializationMode;
        }

        protected override void Analyze(IInvocationExpression invocation,
                                        ElementProblemAnalyzerData data,
                                        IHighlightingConsumer consumer)
        {
            if (!myAssetSerializationMode.IsForceText || !myAssetIndexingSupport.IsEnabled.Value) return;

            var argument = GetStateNameArgumentFrom(invocation);
            if (!(argument?.Value is ICSharpLiteralExpression literal) ||
                !invocation.InvocationExpressionReference.IsAnimatorPlayMethod()) return;
            var container = invocation.GetSolution().TryGetComponent<AnimatorScriptUsagesElementContainer>();
            if (container == null ||
                !literal.ConstantValue.IsNotNullString(out var stateName) ||
                container.ContainsStateName(stateName)) return;
            consumer.AddHighlighting(new UnknownAnimatorStateNameWarning(argument));
        }

        private static ICSharpArgument? GetStateNameArgumentFrom(IInvocationExpression invocationExpression)
        {
            return invocationExpression
                .ArgumentList?
                .Arguments
                .FirstOrDefault(t =>
                    t != null &&
                    (t.IsNamedArgument && t.ArgumentName?.Equals("stateName") == true || !t.IsNamedArgument));
        }
    }
}
