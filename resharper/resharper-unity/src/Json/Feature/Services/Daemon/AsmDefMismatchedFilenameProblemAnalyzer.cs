using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Json.Daemon.Errors;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.Daemon
{
    [ElementProblemAnalyzer(typeof(IJavaScriptLiteralExpression))]
    public class AsmDefMismatchedFilenameProblemAnalyzer : AsmDefProblemAnalyzer<IJavaScriptLiteralExpression>
    {
        protected override void Analyze(IJavaScriptLiteralExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (element.IsNameStringLiteralValue() && data.SourceFile != null)
            {
                var assemblyName = element.GetUnquotedText();
                var expectedFileName = assemblyName + ".asmdef";
                if (expectedFileName != data.SourceFile.Name)
                    consumer.AddHighlighting(new MismatchedAsmDefFilenameWarning(element));
            }
        }
    }
}