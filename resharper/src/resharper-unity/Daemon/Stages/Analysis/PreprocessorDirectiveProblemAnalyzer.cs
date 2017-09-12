using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IPreprocessorDirective), HighlightingTypes = new[]
    {
        typeof(ShaderLabErrorPreprocessorDirectiveError),
        typeof(ShaderLabWarningPreprocessorDirectiveWarning),
        typeof(ShaderLabSyntaxError),
        typeof(ShaderLabSwallowedPreprocessorCharWarning)
    })]
    public class PreprocessorDirectiveProblemAnalyzer : UnityElementProblemAnalyzer<IPreprocessorDirective>
    {
        public PreprocessorDirectiveProblemAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IPreprocessorDirective element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var errorDirective = element as IPpErrorDirective;
            if (errorDirective != null)
                AnalyzeErrorDirective(errorDirective, data, consumer);

            var warningDirective = element as IPpWarningDirective;
            if (warningDirective != null)
                AnalyzeWarningDirective(warningDirective, data, consumer);

            var lineDirective = element as IPpLineDirective;
            if (lineDirective != null)
                AnalyzeLineDirective(lineDirective, data, consumer);
        }

        private void AnalyzeErrorDirective(IPpErrorDirective errorDirective, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (errorDirective.Message == null)
            {
                consumer.AddHighlighting(new ShaderLabSyntaxError("#error not terminated",
                    errorDirective.Directive.GetHighlightingRange()));
            }
            else
            {
                consumer.AddHighlighting(new ShaderLabErrorPreprocessorDirectiveError(errorDirective,
                    errorDirective.Message.GetText().Trim()));
            }
        }

        private void AnalyzeWarningDirective(IPpWarningDirective warningDirective, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (warningDirective.Message == null)
            {
                consumer.AddHighlighting(new ShaderLabSyntaxError("#warning not terminated",
                    warningDirective.Directive.GetHighlightingRange()));
            }
            else
            {
                consumer.AddHighlighting(new ShaderLabWarningPreprocessorDirectiveWarning(warningDirective,
                    warningDirective.Message.GetText().Trim()));
            }
        }

        private void AnalyzeLineDirective(IPpLineDirective lineDirective, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            // TODO: Show warning if line digits are missing? Defaults to 0

            if (lineDirective.Swallowed != null)
            {
                // Unity reads all characters after `#line`, until there are no more digits. In so doing, it reads the
                // next character and swallows it. Norally, this is a new line char, so no big deal. But if for some
                // reason the new line is deleted, then Unity will try to parse `#line 23Shader` and fail, because it's
                // swallowed a character and is trying to parse the identifier `hader` instead of the token `Shader`.
                // Let's add a warning to make this more obvious. I'd lay money that no-one will ever use this feature.
                consumer.AddHighlighting(new ShaderLabSwallowedPreprocessorCharWarning(lineDirective.Swallowed));
            }
        }
    }
}