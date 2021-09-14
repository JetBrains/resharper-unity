using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new []
    {
        typeof(UnknownTagWarning)
    })]
    public class CompareTagUnknownTagAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private readonly UnityProjectSettingsCache myProjectSettingsCache;

        public CompareTagUnknownTagAnalyzer(UnityApi unityApi,
                                            UnityProjectSettingsCache projectSettingsCache)
            : base(unityApi)
        {
            myProjectSettingsCache = projectSettingsCache;
        }

        protected override void Analyze(IInvocationExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!myProjectSettingsCache.IsAvailable())
                return;

            if (element.IsCompareTagMethod())
            {
                var argument = element.ArgumentList.Arguments.FirstOrDefault();
                var literal = (argument?.Value as ICSharpLiteralExpression)?.ConstantValue.Value as string;
                if (literal == null)
                    return;

                if (myProjectSettingsCache != null && !myProjectSettingsCache.HasTag(literal))
                    consumer.AddHighlighting(new UnknownTagWarning(argument));
            }
        }
    }
}