#nullable enable
using System;
using System.Collections.Generic;
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
    HighlightingTypes = new[] { typeof(ImplicitlyEnabledShaderKeywordHighlight), typeof(EnabledShaderKeywordHighlight), typeof(DisabledShaderKeywordHighlight), typeof(SuppressedShaderKeywordHighlight) })]
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
            DaemonProcessKind.VISIBLE_DOCUMENT when file.InclusionContext.RootContext.BaseFile is var rootFile && myShaderProgramCache.TryGetShaderProgramInfo(rootFile, out var shaderProgramInfo) 
                => new Process(process, settings, file, shaderProgramInfo, myEnabledShaderKeywordsProvider?.GetEnabledKeywords(rootFile) ?? EmptySet<string>.InstanceSet),
            _ => null
        };

    protected override bool ShouldWorkInNonUserFile() => false;

    private class Process : CppDaemonStageProcessBase
    {
        private readonly ISet<string> myEnabledKeywords;
        private readonly ShaderProgramInfo myShaderProgramInfo;

        public Process(IDaemonProcess process, IContextBoundSettingsStore settingsStore, CppFile file, ShaderProgramInfo shaderProgramInfo, ISet<string> enabledKeywords) : base(process, settingsStore, file)
        {
            myShaderProgramInfo = shaderProgramInfo;
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
                IHighlighting? highlighting;
                if (child.NodeType == CppCompositeNodeTypes.MACRO_REF)
                    highlighting = TryCreateMacroReferenceHighlighting((MacroReference)child);
                else if (child.NodeType == CppTokenNodeTypes.IDENTIFIER)
                    highlighting = TryCreateIdentifierHighlighting((CppIdentifierTokenNode)child);
                else
                    continue;
                if (highlighting != null)
                    consumer.ConsumeHighlighting(new HighlightingInfo(highlighting.CalculateRange(), highlighting));
            }
        }

        private IHighlighting? TryCreateMacroReferenceHighlighting(MacroReference macroReference)
        {
            if (macroReference.GetReferencedSymbol() is { Substitution: "1", HasParameters: false } symbol
                && !symbol.Location.ContainingFile.IsValid()
                && myShaderProgramInfo.HasKeyword(symbol.Name))
            {
                return myEnabledKeywords.Contains(symbol.Name) ? new EnabledShaderKeywordHighlight(macroReference) : new ImplicitlyEnabledShaderKeywordHighlight(macroReference);
            }

            return null;
        }

        private IHighlighting? TryCreateIdentifierHighlighting(CppIdentifierTokenNode identifierNode)
        {
            var keyword = identifierNode.Name;
            if (myShaderProgramInfo.GetShaderFeatures(keyword) is not { Count: > 0 } features)
                return null;
        
            if (myEnabledKeywords.Contains(keyword))
            {
                var suppressors = new List<string>();
                foreach (var feature in features)
                {
                    foreach (var entry in feature.Entries)
                    {
                        if (entry.Keyword == keyword)
                            break;
                        if (myEnabledKeywords.Contains(entry.Keyword))
                            suppressors.Add(entry.Keyword);
                    }
                }
                
                if (suppressors.Count > 0)
                    return new SuppressedShaderKeywordHighlight(identifierNode, suppressors);
            }
            return new DisabledShaderKeywordHighlight(identifierNode);
        }

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