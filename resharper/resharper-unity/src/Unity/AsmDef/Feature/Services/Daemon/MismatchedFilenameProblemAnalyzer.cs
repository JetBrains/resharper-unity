using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.LiveTemplates;
using JetBrains.ReSharper.Psi.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    [ElementProblemAnalyzer(Instantiation.DemandAnyThreadUnsafe, typeof(IJsonNewLiteralExpression),
                            HighlightingTypes = new[] { typeof(MismatchedAsmDefFilenameWarning) })]
    public class MismatchedFilenameProblemAnalyzer : AsmDefProblemAnalyzer<IJsonNewLiteralExpression>
    {
        protected override void Run(IJsonNewLiteralExpression element,
                                    ElementProblemAnalyzerData data,
                                    IHighlightingConsumer consumer)
        {
            if (element.IsNamePropertyValue())
            {
                var expectedFileName = element.GetUnquotedText();
                if (expectedFileName != AsmDefNameMacroDef.Evaluate(data.SourceFile))
                    consumer.AddHighlighting(new MismatchedAsmDefFilenameWarning(element));
            }
        }
    }
}