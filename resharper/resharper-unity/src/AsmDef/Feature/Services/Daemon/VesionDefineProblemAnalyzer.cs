using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    [ElementProblemAnalyzer(typeof(IJsonNewLiteralExpression),
        HighlightingTypes = new[]
        {
            typeof(InvalidVersionDefineSymbolError), typeof(InvalidVersionDefineExpressionError)
        })]
    public class VersionDefineProblemAnalyzer : AsmDefProblemAnalyzer<IJsonNewLiteralExpression>
    {
        protected override void Run(IJsonNewLiteralExpression element,
                                    ElementProblemAnalyzerData data,
                                    IHighlightingConsumer consumer)
        {
            if (element.IsVersionDefinesObjectDefineValue())
            {
                var symbol = element.GetUnquotedText();
                if (!DefineSymbolUtilities.IsValidVersionDefineSymbol(symbol))
                {
                    var range = symbol.Length == 0
                        ? element.GetHighlightingRange()
                        : element.GetUnquotedDocumentRange();
                    consumer.AddHighlighting(new InvalidVersionDefineSymbolError(element, range));
                }
            }
            else if (element.IsVersionDefinesObjectExpressionValue())
            {
                var expression = element.GetUnquotedText();
                if (!UnitySemanticVersionRange.TryParse(expression, out _))
                    consumer.AddHighlighting(new InvalidVersionDefineExpressionError(element));
            }
        }
    }
}