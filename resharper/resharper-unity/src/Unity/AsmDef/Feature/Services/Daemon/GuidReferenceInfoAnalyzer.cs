#nullable enable

using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl.DocumentMarkup.Adornments;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    [ElementProblemAnalyzer(Instantiation.DemandAnyThreadSafe, typeof(IJsonNewLiteralExpression),
        HighlightingTypes =
            new[]
            {
                typeof(GuidReferenceInfo),
                typeof(AsmDefGuidReferenceInlayHintHighlighting),
                typeof(AsmDefGuidReferenceInlayHintContextActionHighlighting)
            })]
    public class GuidReferenceInfoAnalyzer : AsmDefProblemAnalyzer<IJsonNewLiteralExpression>
    {
        protected override bool AcceptsAsmRef => true;

        protected override void Run(IJsonNewLiteralExpression element,
                                    ElementProblemAnalyzerData data,
                                    IHighlightingConsumer consumer)
        {
            if ((element.IsReferencesArrayEntry() || element.IsReferencePropertyValue())
                && AsmDefUtils.IsGuidReference(element.GetUnquotedText()))
            {
                var reference = element.FindReference<AsmDefNameReference>();
                var declaredElement = reference?.Resolve().DeclaredElement;
                if (declaredElement != null)
                {
                    consumer.AddHighlighting(new GuidReferenceInfo(element, declaredElement.ShortName));

                    var mode = ElementProblemAnalyzerUtils.GetInlayHintsMode(data,
                        settings => settings.ShowAsmDefGuidReferenceNames);
                    if (mode != PushToHintMode.Never)
                    {
                        var documentOffset = element.GetDocumentEndOffset();

                        // This highlight adds the inlay. It's always added but not always visible for push-to-hint
                        consumer.AddHighlighting(new AsmDefGuidReferenceInlayHintHighlighting(documentOffset,
                            $"({declaredElement.ShortName})", mode));

                        // This highlight adds alt+enter context actions to configure the inlay. It's separate so that
                        // we don't get alt+enter actions for an invisible push-to-hint inlay
                        if (mode == PushToHintMode.Always)
                        {
                            consumer.AddHighlighting(
                                new AsmDefGuidReferenceInlayHintContextActionHighlighting(element.GetHighlightingRange()));
                        }
                    }
                }
            }
        }
    }
}