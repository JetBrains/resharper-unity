using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Plugins.Unity.CSharp;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.CSharp.CodeCompletion
{
    [Language(typeof(CSharpLanguage))]
    public class UnityCSharpShaderKeywordsCompletionProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
    {
        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
        }

        protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
        {
            if (!UnityCompletionUtils.IsSpecificArgumentInSpecificMethod(context, out _, out _,
                    methodChecker: ExpressionReferenceUtils.IsGlobalMethodCreate, (_, _) => true) &&
                !UnityCompletionUtils.IsSpecificArgumentInConstructor(context,
                    methodChecker: ExpressionReferenceUtils.IsLocalKeywordConstructor, UnityCompletionUtils.IsCorrespondingArgument("name", 1)) &&
                !UnityCompletionUtils.IsSpecificArgumentInConstructor(context,
                    methodChecker: ExpressionReferenceUtils.IsGlobalKeywordConstructor, (_, _) => true)
                )
                return false;
            
            
            bool hasItems = false;
            context.NodeInFile.GetSolution().GetComponent<ShaderProgramCache>().ForEachKeyword(keyword =>
            {
                hasItems = true;
                var item = new StringLiteralItem($"\"{keyword}\"");
                item.InitializeRanges(context.CompletionRanges, context.BasicContext);
                collector.Add(item);
            });
                
            return hasItems;

        }
        
        
        // TODO why is it copy-pasted across all completion providers?
        private sealed class StringLiteralItem : TextLookupItemBase, IMLSortingAwareItem
        {
            public StringLiteralItem([NotNull] string text)
            {
                Text = text;
            }

            public override IconId Image => PsiSymbolsThemedIcons.Const.Id;

            public override MatchingResult Match(PrefixMatcher prefixMatcher)
            {
                var matchingResult = prefixMatcher.Match(Text);
                if (matchingResult == null)
                    return null;
                return new MatchingResult(matchingResult.MatchedIndices, matchingResult.AdjustedScore - 100,
                    matchingResult.OriginalScore);
            }

            public override void Accept(
                ITextControl textControl, DocumentRange nameRange, LookupItemInsertType insertType,
                Suffix suffix, ISolution solution, bool keepCaretStill)
            {
                base.Accept(textControl, nameRange, LookupItemInsertType.Replace, suffix, solution, keepCaretStill);
            }

            public bool UseMLSort() => false;
        }

    }
}
