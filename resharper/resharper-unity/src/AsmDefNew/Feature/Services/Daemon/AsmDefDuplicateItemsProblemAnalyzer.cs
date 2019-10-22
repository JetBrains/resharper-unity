using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi.Impl.ControlFlow.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Feature.Services.Daemon
{
    // ReSharper doesn't honour the "uniqueItems" schema assertion, so we'll
    // do a quick and dirty version for asmdef files. See RSRP-467118
    [ElementProblemAnalyzer(typeof(IJsonNewMember))]
    public class AsmDefDuplicateItemsProblemAnalyzer : AsmDefProblemAnalyzer<IJsonNewMember>
    {
        public const string AsmDefDuplicateItemDescription = "Array items should be unique";

        protected override void Analyze(IJsonNewMember element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (element.Key == "includePlatforms" || element.Key == "excludePlatforms" ||
                element.Key == "references")
            {
                if (!(element.Value is IJsonNewArray arrayLiteral))
                    return;

                var elements = arrayLiteral.Values;
                if (elements.Count <= 1)
                    return;

                var items = new OneToCompactListMap<string, IJsonNewLiteralExpression>();
                foreach (var expressionOrSpread in elements)
                {
                    if (expressionOrSpread is IJsonNewLiteralExpression literalExpression)
                    {
                        var value = literalExpression.GetStringValue();
                        if (value != null)
                            items.Add(value, literalExpression);
                    }
                }

//                foreach (var item in items)
//                {
//                    var expressions = item.Value;
//                    if (expressions.Count > 1)
//                    {
//                        foreach (var expression in expressions)
//                        {
//                            // We can cheat. We know that `includePlatforms` and `excludePlatforms` are enums
//                            // and get a quick fix to change it to another value
//                            var fixableKind = element.Key == "references"
//                                ? FixableIssueKind.None
//                                : FixableIssueKind.NonEnumValue;
//
//                            var result = new AssertionResult(false, AsmDefDuplicateItemDescription, expression,
//                                fixableIssueKind: fixableKind);
//                            var warning = new JsonValidationFailedWarning(result.Node, result.Description, result);
//                            consumer.AddHighlighting(warning);
//                        }
//                    }
//                }
            }
        }
    }
}
