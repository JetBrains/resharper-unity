using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IPreprocessorDirective), HighlightingTypes = new[]
    {
        typeof(ShaderLabPreprocessorDirectiveError),
        typeof(ShaderLabPreprocessorDirectiveWarning),
        typeof(ShaderLabSyntaxError),
        typeof(ShaderLabSwallowedPreprocessorCharWarning)
    })]
    public class PreprocessorDirectiveProblemAnalyzer : ShaderLabElementProblemAnalyzer<IPreprocessorDirective>
    {
        protected override void Analyze(IPreprocessorDirective element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (element is IPpErrorDirective errorDirective)
                AnalyzeErrorDirective(errorDirective, consumer);

            if (element is IPpWarningDirective warningDirective)
                AnalyzeWarningDirective(warningDirective, consumer);

            if (element is IPpLineDirective lineDirective)
                AnalyzeLineDirective(lineDirective, consumer);
        }

        private void AnalyzeErrorDirective(IPpErrorDirective errorDirective, IHighlightingConsumer consumer)
        {
            if (errorDirective.Message == null)
            {
                consumer.AddHighlighting(new ShaderLabSyntaxError("#error not terminated",
                    errorDirective.Directive.GetHighlightingRange()));
            }
            else
            {
                consumer.AddHighlighting(new ShaderLabPreprocessorDirectiveError(errorDirective,
                    errorDirective.Message.GetText().Trim()));
            }
        }

        private void AnalyzeWarningDirective(IPpWarningDirective warningDirective, IHighlightingConsumer consumer)
        {
            if (warningDirective.Message == null)
            {
                consumer.AddHighlighting(new ShaderLabSyntaxError("#warning not terminated",
                    warningDirective.Directive.GetHighlightingRange()));
            }
            else
            {
                consumer.AddHighlighting(new ShaderLabPreprocessorDirectiveWarning(warningDirective,
                    warningDirective.Message.GetText().Trim()));
            }
        }

        private void AnalyzeLineDirective(IPpLineDirective lineDirective, IHighlightingConsumer consumer)
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