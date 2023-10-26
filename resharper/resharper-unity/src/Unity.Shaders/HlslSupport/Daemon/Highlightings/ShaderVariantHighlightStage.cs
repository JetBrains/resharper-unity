#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.Cpp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Threading;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;

[DaemonStage(StagesBefore = new[] { typeof(GlobalFileStructureCollectorStage), typeof(CppIdentifierHighlightingStage) }, 
    HighlightingTypes = new[] { typeof(ActiveShaderKeywordHighlight), typeof(InactiveShaderKeywordHighlight), typeof(SuppressedShaderKeywordHighlight) })]
public class ShaderVariantHighlightStage : CppDaemonStageBase
{
    private readonly ShaderProgramCache myShaderProgramCache;
    private readonly IEnabledShaderKeywordsProvider? myEnabledShaderKeywordsProvider;
    
    public ShaderVariantHighlightStage(ElementProblemAnalyzerRegistrar elementProblemAnalyzerRegistrar, ShaderProgramCache shaderProgramCache, [Optional] IEnabledShaderKeywordsProvider? enabledShaderKeywordsProvider) : base(elementProblemAnalyzerRegistrar)
    {
        myShaderProgramCache = shaderProgramCache;
        myEnabledShaderKeywordsProvider = enabledShaderKeywordsProvider;
    }

    protected override IDaemonStageProcess? CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings, DaemonProcessKind processKind, CppFile file) =>
        processKind switch
        {
            DaemonProcessKind.VISIBLE_DOCUMENT when file.InclusionContext.RootContext.BaseFile is var rootFile && myShaderProgramCache.TryGetShaderProgramInfo(rootFile, out var shaderProgramInfo) => new Process(process, settings, file, shaderProgramInfo, myEnabledShaderKeywordsProvider?.GetEnabledKeywords(rootFile) ?? EmptySet<string>.InstanceSet),
            _ => null
        };

    protected override bool ShouldWorkInNonUserFile() => false;

    private class Process : CppDaemonStageProcessBase
    {
        private readonly HashSet<string> myKeywordToFeature;
        private readonly ISet<string> myEnabledKeywords;

        public Process(IDaemonProcess process, IContextBoundSettingsStore settingsStore, CppFile file, ShaderProgramInfo shaderProgramInfo, ISet<string> enabledKeywords) : base(process, settingsStore, file)
        {
            myKeywordToFeature = shaderProgramInfo.ShaderFeatures.SelectMany(x => x.Entries.Select(e => e.Keyword)).ToHashSet();
            myEnabledKeywords = enabledKeywords;
        }

        public override void VisitNode(ITreeNode element, IHighlightingConsumer consumer)
        {
            if (element is not Directive { Head.NodeType: {} nodeType } directive)
                return;
            if (!(nodeType == CppTokenNodeTypes.IFDEF_DIRECTIVE || nodeType == CppTokenNodeTypes.IFNDEF_DIRECTIVE || nodeType == CppTokenNodeTypes.IF_DIRECTIVE || nodeType == CppTokenNodeTypes.ELIF_DIRECTIVE)) 
                return;
            
            foreach (var child in directive.MeaningfulChildren())
            {
                if (child.NodeType == CppTokenNodeTypes.END_OF_DIRECTIVE_CONTENT)
                    break;
                IHighlighting highlighting;
                if (child is MacroReference macroReference && IsShaderKeywordReference(macroReference))
                    highlighting = new ActiveShaderKeywordHighlight(macroReference);
                else if (child.NodeType == CppTokenNodeTypes.IDENTIFIER && child.GetText() is var identifier && IsShaderKeyword(identifier))
                    highlighting = myEnabledKeywords.Contains(identifier) ? new SuppressedShaderKeywordHighlight((CppIdentifierTokenNode)child) : new InactiveShaderKeywordHighlight((CppIdentifierTokenNode)child);
                else
                    continue;
                consumer.ConsumeHighlighting(new HighlightingInfo(highlighting.CalculateRange(), highlighting));
            }
        }

        private bool IsShaderKeywordReference(MacroReference macroReference)
        {
            return macroReference.GetReferencedSymbol() is { Substitution: "1", HasParameters: false } symbol 
                   && !symbol.Location.ContainingFile.IsValid()
                   && IsShaderKeyword(symbol.Name);
        }

        private bool IsShaderKeyword(string name) => myKeywordToFeature.Contains(name);

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