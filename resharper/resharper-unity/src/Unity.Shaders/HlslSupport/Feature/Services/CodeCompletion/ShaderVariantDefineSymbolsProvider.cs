#nullable enable
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
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

        var existingDefineSymbols = new HashSet<string>(cppFile.InclusionContext.RootContext.AllDefines.Select(x => x.Name));
        var icon = basicContext.PsiIconManager.GetImage(CppDeclaredElementTypes.MACRO);
        foreach (var keyword in shaderProgramInfo.Keywords) 
            AddLookupItem(context, collector, icon, keyword, existingDefineSymbols);

        foreach (var symbol in ShaderDefineSymbolsRecognizer.KnownDefineSymbols) 
            AddLookupItem(context, collector, icon, symbol, existingDefineSymbols);

        return true;
    }

    private void AddLookupItem(CppCodeCompletionInsideDirectiveContext context, IItemsCollector collector, IconId? icon, string defineSymbol, HashSet<string> existingDefineSymbols)
    {
        if (!existingDefineSymbols.Add(defineSymbol))
            return;

        var info = new MyTextualInfo(icon, defineSymbol) { Ranges = context.CompletionRanges };
        var item = LookupItemFactory.CreateLookupItem(info)
            .WithPresentation(static item => new TextPresentation<TextualInfo>(item.Info, item.Info.Icon, emphasize: false))
            .WithBehavior(static item => new TextualBehavior<TextualInfo>(item.Info))
            .WithMatcher(static item => new MyTextualMatcher(item.Info));
        collector.Add(item);
    }
    
    private class MyTextualMatcher : TextualMatcher<TextualInfo>
    {
        private readonly bool myStartsWithUnderscore;
        
        public MyTextualMatcher(TextualInfo info) : base(info)
        {
            myStartsWithUnderscore = info.Text is { Length: > 0 } text && text[0] is '_';
        }

        public override MatchingResult? Match(PrefixMatcher prefixMatcher)
        {
            if (myStartsWithUnderscore && prefixMatcher.Prefix.Length == 0)
                return null;
            
            return base.Match(prefixMatcher);
        }
    }

    private class MyTextualInfo : TextualInfo
    {
        public IconId? Icon { get; }

        public MyTextualInfo(IconId? icon, string defineSymbol) : base(defineSymbol, defineSymbol)
        {
            Icon = icon;
            Placement.Relevance = (ulong)CppCompletionRanks.Define;
        }
    }
}