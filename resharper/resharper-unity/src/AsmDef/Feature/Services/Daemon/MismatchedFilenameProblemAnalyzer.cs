using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.LiveTemplates;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    [ElementProblemAnalyzer(typeof(IJsonNewLiteralExpression),
                            HighlightingTypes = new[] { typeof(MismatchedAsmDefFilenameWarning) })]
    public class MismatchedFilenameProblemAnalyzer : AsmDefProblemAnalyzer<IJsonNewLiteralExpression>
    {
        protected override void Analyze(IJsonNewLiteralExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (element.IsNamePropertyValue() && data.SourceFile != null)
            {
                var assemblyName = element.GetUnquotedText();
                var expectedFileName = assemblyName;
                if (expectedFileName != AsmDefNameMacroDef.Evaluate(data.SourceFile))
                    consumer.AddHighlighting(new MismatchedAsmDefFilenameWarning(element));
            }
        }
    }
}