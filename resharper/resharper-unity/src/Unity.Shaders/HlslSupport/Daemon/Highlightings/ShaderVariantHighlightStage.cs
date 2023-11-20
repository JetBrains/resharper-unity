#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Cpp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
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
    private readonly UnitySolutionTracker myUnitySolutionTracker;
    private readonly ShaderVariantsManager myShaderVariantsManager;
    
    public ShaderVariantHighlightStage(ElementProblemAnalyzerRegistrar elementProblemAnalyzerRegistrar, ShaderProgramCache shaderProgramCache, UnitySolutionTracker unitySolutionTracker, ShaderVariantsManager shaderVariantsManager) : base(elementProblemAnalyzerRegistrar)
    {
        myShaderProgramCache = shaderProgramCache;
        myUnitySolutionTracker = unitySolutionTracker;
        myShaderVariantsManager = shaderVariantsManager;
    }

    protected override IDaemonStageProcess? CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings, DaemonProcessKind processKind, CppFile file) =>
        processKind switch
        {
            DaemonProcessKind.VISIBLE_DOCUMENT when settings.GetValue((UnitySettings s) => s.FeaturePreviewShaderVariantsSupport) && 
                                                    myUnitySolutionTracker.IsUnityProjectOrHasUnityReference && 
                                                    file.InclusionContext.RootContext is { BaseFile: var rootFile, LanguageDialect: var dialect } && 
                                                    myShaderProgramCache.TryGetShaderProgramInfo(rootFile, out var shaderProgramInfo) 
                => new ShaderKeywordsHighlightProcess(process, settings, file, shaderProgramInfo, myShaderVariantsManager.ShaderApi, myShaderVariantsManager.ShaderPlatform, myShaderVariantsManager.GetEnabledKeywords(rootFile), dialect.Pragmas),
            _ => null
        };

    protected override bool ShouldWorkInNonUserFile() => false;
}

file class ShaderKeywordsHighlightProcess : IDaemonStageProcess, IRecursiveElementProcessor<IHighlightingConsumer>
{
    private readonly ISet<string> myEnabledKeywords;
    private readonly IContextBoundSettingsStore mySettingsStore;
    private readonly ShaderProgramInfo myShaderProgramInfo;
    private readonly ShaderApi myShaderApi;
    private readonly ShaderPlatform myShaderPlatform;
    private readonly IReadOnlyDictionary<string, PragmaCommand> myPragmas;
    private readonly CppFile myFile;
    
    private ITreeNode? myDirectiveNode;

    public IDaemonProcess DaemonProcess { get; }

    public ShaderKeywordsHighlightProcess(IDaemonProcess process, IContextBoundSettingsStore settingsStore, CppFile file, ShaderProgramInfo shaderProgramInfo, ShaderApi shaderApi, ShaderPlatform shaderPlatform, ISet<string> enabledKeywords, IReadOnlyDictionary<string, PragmaCommand> pragmas)
    {
        mySettingsStore = settingsStore;
        myShaderProgramInfo = shaderProgramInfo;
        myShaderApi = shaderApi;
        myShaderPlatform = shaderPlatform;
        myEnabledKeywords = enabledKeywords;
        myPragmas = pragmas;
        myFile = file;
        DaemonProcess = process;
    }
        
    public void Execute(Action<DaemonStageResult> committer)
    {
        using var activityCookie = Interruption.Current.Add(new PollingInterruptionSource(() => DaemonProcess.InterruptFlag));
        using var checkCookie = new CppEnsureCheckForInterruptCookie();
        using var prioritisedCookie = new CppResolveMutexInterruptCookie(myFile);

        using var compilationContextCookie = CompilationContextCookie.OverrideOrCreate(myFile.ResolveContext);

        var consumer = new FilteringHighlightingConsumer(DaemonProcess.SourceFile, myFile, mySettingsStore);
        try
        {
            myFile.ProcessDescendants(this, consumer);
        }
        catch (Exception e) when (!e.IsOperationCanceled())
        {
            e.AddSensitiveData("File", myFile.File.Location);
            throw;
        }
        finally
        {
            myDirectiveNode = null;
        }

        committer(new DaemonStageResult(consumer.Highlightings));
    }

    public bool InteriorShouldBeProcessed(ITreeNode element, IHighlightingConsumer context) => element.NodeType != CppCompositeNodeTypes.MACRO_REF && (myDirectiveNode != null || element is not ICppDirective);
    public bool IsProcessingFinished(IHighlightingConsumer context) => Interruption.Current.Check();

    public void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer consumer)
    {
        if (myDirectiveNode == null)
        {
            switch (element)
            {
                case Directive directive:
                    if (directive.Head.NodeType is { } nodeType && (nodeType == CppTokenNodeTypes.IFDEF_DIRECTIVE || nodeType == CppTokenNodeTypes.IFNDEF_DIRECTIVE || nodeType == CppTokenNodeTypes.IF_DIRECTIVE || nodeType == CppTokenNodeTypes.ELIF_DIRECTIVE))
                        myDirectiveNode = directive;
                    break;
                case PPPragmaDirective pragmaDirective:
                    VisitPragmaDirective(pragmaDirective, consumer);
                    break;
            }
        }
        else
        {
            switch (element)
            {
                case MacroReference macroReference:
                    VisitMacroReference(macroReference, consumer);
                    break;
                case CppIdentifierTokenNode identifier:
                    VisitIdentifier(identifier, consumer);
                    break;
            }
        }
    }

    public void ProcessAfterInterior(ITreeNode element, IHighlightingConsumer context)
    {
        if (ReferenceEquals(element, myDirectiveNode)) 
            myDirectiveNode = null;
    }

    private void VisitPragmaDirective(PPPragmaDirective directive, IHighlightingConsumer consumer)
    {
        if (directive.PragmaNameNode is CppIdentifierTokenNode nameNode
            && myPragmas.TryGetValue(nameNode.Name, out var pragmaCommand)
            && pragmaCommand is ShaderLabPragmaCommand { Info: { DeclaresKeywords: true } info })
        {
            CppIdentifierTokenNode? enabledKeyword = null;
            var items = new LocalList<(CppIdentifierTokenNode Keyword, bool Enabled)>();
            for (var node = nameNode.GetNextMeaningfulSibling(); node != null && node.NodeType != CppTokenNodeTypes.END_OF_DIRECTIVE_CONTENT; node = node.GetNextMeaningfulSibling())
            {
                if (node is not CppIdentifierTokenNode { Name: var keyword } identifier) continue;
                var enabled = myEnabledKeywords.Contains(keyword);
                items.Add((identifier, enabled));
                if (enabled && enabledKeyword == null)
                    enabledKeyword = identifier;
            }

            if (items.Count == 0)
                return;

            var index = 0;
            IHighlighting highlighting;
            if (enabledKeyword == null && (items.Count > 1 || !info.HasDisabledVariantForSingleKeyword))
            {
                highlighting = new ImplicitlyEnabledShaderKeywordHighlight(items[0].Keyword);
                consumer.ConsumeHighlighting(new HighlightingInfo(highlighting.CalculateRange(), highlighting));
                ++index;
            }

            var suppressors = ImmutableArray.CreateBuilder<string>();
            for (; index < items.Count; ++index)
            {
                var item = items[index];
                if (item.Enabled)
                {
                    highlighting = ReferenceEquals(item.Keyword, enabledKeyword) ? new EnabledShaderKeywordHighlight(item.Keyword) : new SuppressedShaderKeywordHighlight(item.Keyword, suppressors.MoveOrCopyToImmutableArray());
                    suppressors.Add(item.Keyword.Name);
                }
                else
                    highlighting = new DisabledShaderKeywordHighlight(item.Keyword);
                consumer.ConsumeHighlighting(new HighlightingInfo(highlighting.CalculateRange(), highlighting));
            }
        }
    }

    private void VisitMacroReference(MacroReference macroReference, IHighlightingConsumer consumer)
    {
        if (macroReference.GetReferencedSymbol() is { Substitution: "1", HasParameters: false } symbol
            && !symbol.Location.ContainingFile.IsValid())
        {
            if (myShaderProgramInfo.HasKeyword(symbol.Name))
                Consume(consumer, myEnabledKeywords.Contains(symbol.Name) ? new EnabledShaderKeywordHighlight(macroReference) : new ImplicitlyEnabledShaderKeywordHighlight(macroReference));
            else if (ShaderDefineSymbolsRecognizer.Recognize(symbol.Name) is {} descriptor)
                Consume(consumer, !descriptor.IsDefaultSymbol(symbol.Name) ? new EnabledShaderKeywordHighlight(macroReference) : new ImplicitlyEnabledShaderKeywordHighlight(macroReference));
        }
    }

    private void VisitIdentifier(CppIdentifierTokenNode identifierNode, IHighlightingConsumer consumer)
    {
        var symbol = identifierNode.Name;
        if (myShaderProgramInfo.GetShaderFeatures(symbol) is { Count: > 0 } features)
            Consume(consumer, GetKeywordIdentifierHighlighting(identifierNode, features));
        else if (ShaderDefineSymbolsRecognizer.Recognize(symbol) is {} descriptor)
            ConsumeShaderDefineSymbolIdentifier(identifierNode, descriptor, consumer);
    }

    private void ConsumeShaderDefineSymbolIdentifier(CppIdentifierTokenNode identifierNode, IShaderDefineSymbolDescriptor descriptor, IHighlightingConsumer consumer)
    {
        var symbol = identifierNode.Name;
        var isEnabled = descriptor switch
        {
            ShaderApiDefineSymbolDescriptor shaderApiDescriptor => shaderApiDescriptor.GetValue(symbol) == myShaderApi,
            ShaderPlatformDefineSymbolDescriptor shaderPlatformDescriptor => shaderPlatformDescriptor.GetValue(symbol) == myShaderPlatform,
            _ => false
        };
        if (!isEnabled)
            Consume(consumer, new DisabledShaderKeywordHighlight(identifierNode));
        else if (descriptor.IsDefaultSymbol(symbol))
            Consume(consumer, new ImplicitlyEnabledShaderKeywordHighlight(identifierNode));
        else
            Consume(consumer, new EnabledShaderKeywordHighlight(identifierNode));
    }

    private IHighlighting GetKeywordIdentifierHighlighting(CppIdentifierTokenNode identifierNode, OneToListMap<string, ShaderFeature>.ValueCollection features)
    {
        var keyword = identifierNode.Name;
        if (myEnabledKeywords.Contains(keyword))
        {
            var suppressors = ImmutableArray.CreateBuilder<string>();
            foreach (var feature in features)
            {
                var suppressedInFeature = false;
                foreach (var entry in feature.Entries)
                {
                    if (entry.Keyword == keyword)
                    {
                        if (!suppressedInFeature)
                            return new EnabledShaderKeywordHighlight(identifierNode);
                        break;
                    }
                    if (myEnabledKeywords.Contains(entry.Keyword))
                    {
                        suppressors.Add(entry.Keyword);
                        suppressedInFeature = true;
                    }
                }
            }
            
            return new SuppressedShaderKeywordHighlight(identifierNode, suppressors.MoveOrCopyToImmutableArray());
        }

        foreach (var feature in features)
        {
            if (feature is { AllowAllDisabled: false, Entries.Length: > 0 } && feature.Entries[0].Keyword == keyword)
            {
                var otherKeywordEnabled = false;
                for (var i = 1; i < feature.Entries.Length; ++i)
                {
                    if (myEnabledKeywords.Contains(feature.Entries[i].Keyword))
                    {
                        otherKeywordEnabled = true;
                        break;
                    }
                }
                if (!otherKeywordEnabled)
                    return new ImplicitlyEnabledShaderKeywordHighlight(identifierNode);
            }
        }

        return new DisabledShaderKeywordHighlight(identifierNode);
    }

    private void Consume(IHighlightingConsumer consumer, IHighlighting highlighting) => consumer.ConsumeHighlighting(new HighlightingInfo(highlighting.CalculateRange(), highlighting));
}