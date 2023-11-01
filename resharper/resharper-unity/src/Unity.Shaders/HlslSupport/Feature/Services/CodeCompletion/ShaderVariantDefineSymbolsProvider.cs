#nullable enable
using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeCompletion;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Feature.Services.CodeCompletion;

[Language(typeof(CppLanguage))]
public class ShaderVariantDefineSymbolsProvider : ItemsProviderOfSpecificContext<CppCodeCompletionInsideDirectiveContext>
{
    public override bool IsFinal => true;

    protected override bool IsAvailable(CppCodeCompletionInsideDirectiveContext context) =>
        (context.BasicContext.File as CppFile)?.GetLanguageDialect() is UnityHlslDialectBase &&
        context.ReparseContext.TreeNode is CppIdentifierTokenNode && 
        context.Directive is Directive directive && 
        HlslUtils.IsConditionalDirective(directive);
    
    protected override bool AddLookupItems(CppCodeCompletionInsideDirectiveContext context, IItemsCollector collector)
    {
        var basicContext = context.BasicContext;
        if (basicContext.File is not CppFile cppFile)
            return false;
        
        var location = cppFile.InclusionContext.RootContext.BaseFile;
        var shaderProgramCache = context.BasicContext.Solution.GetComponent<ShaderProgramCache>();
        if (!shaderProgramCache.TryGetOrReadUpToDateProgramInfo(basicContext.SourceFile, location, out var shaderProgramInfo))
            return false;

        var existingDefineSymbols = new HashSet<string>();
        foreach (var item in collector.Items)
        {
            if ((item.Placement.Relevance & (ulong)CppCompletionRanks.Define) != 0)
                existingDefineSymbols.Add(item.Placement.OrderString);
        }
        
        foreach (var keyword in shaderProgramInfo.Keywords) 
            AddLookupItem(context, collector, keyword, existingDefineSymbols);

        foreach (var symbol in ShaderDefineSymbolsRecognizer.KnownDefineSymbols) 
            AddLookupItem(context, collector, symbol, existingDefineSymbols);

        return true;
    }

    private void AddLookupItem(CppCodeCompletionInsideDirectiveContext context, IItemsCollector collector, string defineSymbol, HashSet<string> existingDefineSymbols)
    {
        if (!existingDefineSymbols.Add(defineSymbol))
            return;
        
        var info = new MyTextualInfo(defineSymbol) { Ranges = context.CompletionRanges };
        var item = LookupItemFactory.CreateLookupItem(info)
            .WithPresentation(static item => new TextPresentation<TextualInfo>(item.Info, item.Info.Icon, emphasize: false))
            .WithBehavior(static item => new TextualBehavior<TextualInfo>(item.Info))
            .WithMatcher(static item => new TextualMatcher<TextualInfo>(item.Info));
        collector.Add(item);
    }

    public class MyTextualInfo : TextualInfo
    {
        public IconId? Icon => null;

        public MyTextualInfo(string defineSymbol) : base(defineSymbol, defineSymbol)
        {
            Placement.Relevance = (ulong)CppCompletionRanks.Define;
        }
    }
}