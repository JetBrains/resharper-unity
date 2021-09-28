using System;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    [ElementProblemAnalyzer(typeof(IJsonNewLiteralExpression),
        HighlightingTypes = new[] { typeof(AsmDefGuidReferenceInfo) })]
    public class AsmDefGuidReferenceInfoAnalyzer : AsmDefProblemAnalyzer<IJsonNewLiteralExpression>
    {
        protected override void Analyze(IJsonNewLiteralExpression element, ElementProblemAnalyzerData data,
                                        IHighlightingConsumer consumer)
        {
            if (element.IsReferenceLiteral() && element.GetUnquotedText()
                .StartsWith("guid:", StringComparison.InvariantCultureIgnoreCase))
            {
                var reference = element.FindReference<AsmDefNameReference>();
                var declaredElement = reference?.Resolve().DeclaredElement;
                if (declaredElement != null)
                    consumer.AddHighlighting(new AsmDefGuidReferenceInfo(element, declaredElement.ShortName));
            }
        }
    }
}