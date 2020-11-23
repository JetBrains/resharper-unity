using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages;
using JetBrains.ReSharper.Plugins.Yaml.Settings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression),
        HighlightingTypes = new[] {typeof(UnknownAnimatorStateNameWarning)})]
    public class PlayAnimatorStateAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        [NotNull] private readonly AssetSerializationMode myAssetSerializationMode;
        [NotNull] private readonly YamlSupport myUnityYamlSupport;

        public PlayAnimatorStateAnalyzer([NotNull] UnityApi unityApi,
                                         [NotNull] AssetSerializationMode assetSerializationMode,
                                         [NotNull] YamlSupport unityYamlSupport)
            : base(unityApi)
        {
            myAssetSerializationMode = assetSerializationMode;
            myUnityYamlSupport = unityYamlSupport;
        }

        protected override void Analyze([NotNull] IInvocationExpression invocation,
                                        ElementProblemAnalyzerData data,
                                        [NotNull] IHighlightingConsumer consumer)
        {
            if (!myAssetSerializationMode.IsForceText) return;
            var boxedIsParsingEnabled = myUnityYamlSupport.IsParsingEnabled;
            if (boxedIsParsingEnabled is null || !boxedIsParsingEnabled.Value) return;
            var argument = GetStateNameArgumentFrom(invocation);
            if (!(argument?.Value is ICSharpLiteralExpression literal) ||
                !invocation.InvocationExpressionReference.IsAnimatorPlayMethod()) return;
            var container = invocation.GetSolution().TryGetComponent<AnimatorScriptUsagesElementContainer>();
            if (container == null || container.GetStateNames().Contains(literal.ConstantValue.Value)) return;
            consumer.AddHighlighting(new UnknownAnimatorStateNameWarning(argument));
        }

        [CanBeNull]
        private static ICSharpArgument GetStateNameArgumentFrom([NotNull] IInvocationExpression invocationExpression)
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