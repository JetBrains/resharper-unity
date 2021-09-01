using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    [ElementProblemAnalyzer(typeof(IJsonNewLiteralExpression),
                            HighlightingTypes = new[] { typeof(MismatchedAsmDefFilenameWarning) })]
    public class AsmDefMismatchedFilenameProblemAnalyzer : AsmDefProblemAnalyzer<IJsonNewLiteralExpression>
    {
        protected override void Analyze(IJsonNewLiteralExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (element.IsNameLiteral() && data.SourceFile != null)
            {
                var assemblyName = element.GetUnquotedText();
                var expectedFileName = assemblyName + ".asmdef";
                if (expectedFileName != data.SourceFile.Name)
                    consumer.AddHighlighting(new MismatchedAsmDefFilenameWarning(element));
            }
        }
    }
}