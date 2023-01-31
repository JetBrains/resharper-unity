using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new []
    {
        typeof(UnknownTagWarning)
    })]
    public class CompareTagUnknownTagAnalyzer : ProjectSettingsRelatedProblemAnalyzerBase<IInvocationExpression>
    {
        public CompareTagUnknownTagAnalyzer(UnityApi unityApi,
                                            UnityProjectSettingsCache projectSettingsCache)
            : base(unityApi, projectSettingsCache)
        {
        }

        protected override void Analyze(IInvocationExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (element.IsCompareTagMethod() || element.IsFindObjectByTagMethod())
            {
                // TODO: Use conditional access when the monorepo build uses a more modern C# compiler
                // Currently (as of 01/2023) the monorepo build for Unity uses C#9 compiler, which will complain that
                // the out variable is uninitialised when we use conditional access
                // See also https://youtrack.jetbrains.com/issue/RSRP-489147
                var argument = element.ArgumentList.Arguments.FirstOrDefault();
                if (argument?.Value != null && argument.Value.ConstantValue.IsNotNullString(out var literal)
                                            && !ProjectSettingsCache.HasTag(literal))
                {
                    consumer.AddHighlighting(new UnknownTagWarning(argument));
                }
            }
        }
    }
}
