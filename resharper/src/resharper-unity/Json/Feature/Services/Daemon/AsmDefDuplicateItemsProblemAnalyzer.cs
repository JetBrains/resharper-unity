using JetBrains.ReSharper.Daemon.JavaScript.Stages;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Impl.ControlFlow.Util;
using JetBrains.ReSharper.Psi.JavaScript.Services.Json;
using JetBrains.ReSharper.Psi.JavaScript.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.Daemon
{
    // ReSharper doesn't honour the "uniqueItems" schema assertion, so we'll
    // do a quick and dirty version for asmdef files. See RSRP-467118
    [ElementProblemAnalyzer(typeof(IObjectPropertyInitializer))]
    public class AsmDefDuplicateItemsProblemAnalyzer : AsmDefProblemAnalyzer<IObjectPropertyInitializer>
    {
        public const string AsmDefDuplicateItemDescription = "Array items should be unique";

        protected override void Analyze(IObjectPropertyInitializer element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (element.DeclaredName == "includePlatforms" || element.DeclaredName == "excludePlatforms" ||
                element.DeclaredName == "references")
            {
                if (!(element.Value is IArrayLiteral arrayLiteral))
                    return;

                var elements = arrayLiteral.ArrayElements;
                if (elements.Count <= 1)
                    return;

                var items = new OneToCompactListMap<string, IJavaScriptLiteralExpression>();
                foreach (var expressionOrSpread in elements)
                {
                    if (expressionOrSpread is IJavaScriptLiteralExpression literalExpression)
                    {
                        var value = literalExpression.GetStringValue();
                        if (value != null)
                            items.Add(value, literalExpression);
                    }
                }

                foreach (var item in items)
                {
                    var expressions = item.Value;
                    if (expressions.Count > 1)
                    {
                        foreach (var expression in expressions)
                        {
                            // We can cheat. We know that `includePlatforms` and `excludePlatforms` are enums
                            // and get a quick fix to change it to another value
                            var fixableKind = element.DeclaredName == "references"
                                ? FixableIssueKind.None
                                : FixableIssueKind.NonEnumValue;

                            var result = new AssertionResult(false, AsmDefDuplicateItemDescription, expression,
                                fixableIssueKind: fixableKind);
                            var warning = new JsonValidationFailedWarning(result.Node, result.Description, result);
                            consumer.AddHighlighting(warning);
                        }
                    }
                }
            }
        }
    }
}
