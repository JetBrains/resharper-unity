using System;
using JetBrains.Application.InlayHints;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    [ElementProblemAnalyzer(typeof(IJsonNewLiteralExpression),
        HighlightingTypes =
            new[]
            {
                typeof(GuidReferenceInfo),
                typeof(AsmDefGuidReferenceInlayHintHighlighting),
                typeof(AsmDefGuidReferenceHintContextActionHighlighting)
            })]
    public class GuidReferenceInfoAnalyzer : AsmDefProblemAnalyzer<IJsonNewLiteralExpression>
    {
        protected override void Analyze(IJsonNewLiteralExpression element, ElementProblemAnalyzerData data,
                                        IHighlightingConsumer consumer)
        {
            if (element.IsReferencesArrayEntry() && element.GetUnquotedText().StartsWith("guid:",
                StringComparison.InvariantCultureIgnoreCase))
            {
                var reference = element.FindReference<AsmDefNameReference>();
                var declaredElement = reference?.Resolve().DeclaredElement;
                if (declaredElement != null)
                {
                    consumer.AddHighlighting(new GuidReferenceInfo(element, declaredElement.ShortName));

                    var mode = GetMode(data);
                    if (mode != InlayHintsMode.Never)
                    {
                        var documentOffset = element.GetDocumentEndOffset();

                        // This highlight adds the inlay. It's always added but not always visible for push-to-hint
                        consumer.AddHighlighting(new AsmDefGuidReferenceInlayHintHighlighting(documentOffset,
                            $"({declaredElement.ShortName})", mode));

                        // This highlight adds alt+enter context actions to configure the inlay. It's separate so that
                        // we don't get alt+enter actions for an invisible push-to-hint inlay
                        if (mode == InlayHintsMode.Always)
                        {
                            consumer.AddHighlighting(
                                new AsmDefGuidReferenceHintContextActionHighlighting(element.GetHighlightingRange()));
                        }
                    }
                }
            }
        }

        private static InlayHintsMode GetMode(ElementProblemAnalyzerData data)
        {
            if (data.RunKind != ElementProblemAnalyzerRunKind.FullDaemon)
                return InlayHintsMode.Never;

            if (data.GetDaemonProcessKind() != DaemonProcessKind.VISIBLE_DOCUMENT)
                return InlayHintsMode.Never;

            // This checks the "Enable Inlay Hints in .NET languages" option. It's a stretch to call .asmdef a .net
            // language, but it's the best overall switch we've got
            if (!data.SettingsStore.GetValue((GeneralInlayHintsOptions s) => s.EnableInlayHints))
                return InlayHintsMode.Never;

            return data.SettingsStore.GetValue((UnityInlayHintSettings s) => s.ShowAsmDefGuidReferenceNames)
                .EnsureDefault(data.SettingsStore);
        }
    }
}