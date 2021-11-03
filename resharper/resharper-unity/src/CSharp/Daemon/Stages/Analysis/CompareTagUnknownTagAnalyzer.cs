using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
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
            if (element.IsCompareTagMethod())
            {
                var argument = element.ArgumentList.Arguments.FirstOrDefault();
                var literal = (argument?.Value as ICSharpLiteralExpression)?.ConstantValue.Value as string;
                if (literal == null)
                    return;

                if (!ProjectSettingsCache.HasTag(literal))
                    consumer.AddHighlighting(new UnknownTagWarning(argument));
            }
        }
    }
}