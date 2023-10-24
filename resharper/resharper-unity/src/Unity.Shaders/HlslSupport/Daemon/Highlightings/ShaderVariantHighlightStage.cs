#nullable enable
using System;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.Cpp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Threading;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;

[DaemonStage(StagesBefore = new[] { typeof(GlobalFileStructureCollectorStage), typeof(CppIdentifierHighlightingStage) }, HighlightingTypes = new[] { typeof(ShaderKeywordHighlight) })]
public class ShaderVariantHighlightStage : CppDaemonStageBase
{
    public ShaderVariantHighlightStage(ElementProblemAnalyzerRegistrar elementProblemAnalyzerRegistrar) : base(elementProblemAnalyzerRegistrar)
    {
    }

    protected override IDaemonStageProcess? CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings, DaemonProcessKind processKind, CppFile file) =>
        processKind switch
        {
            DaemonProcessKind.VISIBLE_DOCUMENT => new Process(process, settings, file),
            _ => null
        };

    protected override bool ShouldWorkInNonUserFile() => false;

    private class Process : CppDaemonStageProcessBase
    {
        public Process(IDaemonProcess process, IContextBoundSettingsStore settingsStore, CppFile file) : base(process, settingsStore, file)
        {
        }

        public override void VisitNode(ITreeNode element, IHighlightingConsumer consumer)
        {
            if (element is not Directive { Head.NodeType: {} nodeType } directive)
                return;
            if (nodeType == CppTokenNodeTypes.IFDEF_DIRECTIVE || nodeType == CppTokenNodeTypes.IFNDEF_DIRECTIVE || nodeType == CppTokenNodeTypes.IF_DIRECTIVE)
            {
                foreach (var child in directive.MeaningfulChildren())
                {
                    if (child.NodeType == CppTokenNodeTypes.END_OF_DIRECTIVE_CONTENT)
                        break;
                    if (child is MacroReference macroReference && IsShaderVariantReference(macroReference))
                    {
                        var highlighting = new ShaderKeywordHighlight(macroReference);
                        consumer.ConsumeHighlighting(new HighlightingInfo(highlighting.CalculateRange(), highlighting));
                    }
                }
            }
        }

        private bool IsShaderVariantReference(MacroReference macroReference) => macroReference.GetReferencedSymbol() is { Substitution: "1", HasParameters: false } symbol && !symbol.Location.ContainingFile.IsValid();

        public override void Execute(Action<DaemonStageResult> committer)
        {
            using var activityCookie = Interruption.Current.Add(new PollingInterruptionSource(() => DaemonProcess.InterruptFlag));
            using var checkCookie = new CppEnsureCheckForInterruptCookie();
            using var prioritisedCookie = new CppResolveMutexInterruptCookie(File);

            using var compilationContextCookie = CompilationContextCookie.OverrideOrCreate(File.ResolveContext);

            var consumer = new FilteringHighlightingConsumer(DaemonProcess.SourceFile, File, DaemonProcess.ContextBoundSettingsStore);
            try
            {
                File.ProcessDescendants(this, consumer);
            }
            catch (Exception e) when (!e.IsOperationCanceled())
            {
                e.AddSensitiveData("File", File.File.Location);
                throw;
            }

            Commit(consumer, committer, DocumentRange.InvalidRange);
        }
    }
}