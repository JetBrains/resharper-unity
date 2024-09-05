using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Psi.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    [ElementProblemAnalyzer(Instantiation.DemandAnyThreadSafe, typeof(IJsonNewLiteralExpression),
        HighlightingTypes = new[] { typeof(InvalidDefineConstraintExpressionError) })]
    public class DefineConstraintsProblemAnalyzer : AsmDefProblemAnalyzer<IJsonNewLiteralExpression>
    {
        protected override void Run(IJsonNewLiteralExpression element,
                                    ElementProblemAnalyzerData data,
                                    IHighlightingConsumer consumer)
        {
            if (!element.IsDefineConstraintsArrayEntry())
                return;

            var expression = element.GetUnquotedText();
            if (!DefineSymbolUtilities.IsValidDefineConstraintExpression(expression))
            {
                var range = expression.Length == 0
                    ? element.GetHighlightingRange()
                    : element.GetUnquotedDocumentRange();
                consumer.AddHighlighting(new InvalidDefineConstraintExpressionError(element, range));
            }
        }
    }
}