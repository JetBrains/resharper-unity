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
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion
{
    [Language(typeof(CSharpLanguage))]
    public class UnityTagsCompletionProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
    {
        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
        }
        
        protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
        {
            if (!IsTagRelatedMethod(context)) 
                return false;
            
            var cache = context.NodeInFile.GetSolution().GetComponent<UnityProjectSettingsCache>();
            var hasItems = false;
            foreach (var sceneName in cache.GetAllTags())
            {
                var item = new StringLiteralItem($"\"{sceneName}\"");
                item.InitializeRanges(context.CompletionRanges, context.BasicContext);
                collector.Add(item);
                hasItems = true;
            }

            return hasItems;
        }

        private static bool IsTagRelatedMethod(CSharpCodeCompletionContext context)
        {
            // tag completion, tag == "..."
            if (IsTagEquality(context, out _))
                return true;

            // tag completion, CompareTag("...")
            if (UnityCompletionUtils.IsSpecificArgumentInSpecificMethod(context, out _, out _,
                    ExpressionReferenceUtils.IsCompareTagMethod,
                    UnityCompletionUtils.IsCorrespondingArgument("tag")))
                
                return true;

            // tag completion, GameObject.FindWithTag("...")
            if (UnityCompletionUtils.IsSpecificArgumentInSpecificMethod(context, out _, out _,
                    ExpressionReferenceUtils.IsFindObjectByTagMethod,
                    UnityCompletionUtils.IsCorrespondingArgument("tag")))
                return true;

            return false;
        }


        private static bool IsTagEquality(CSharpCodeCompletionContext context, out ICSharpLiteralExpression stringLiteral)
        {
            stringLiteral = null;
            var nodeInFile = context.NodeInFile;
            var eqExpression = nodeInFile.NextSibling as IEqualityExpression ??
                               nodeInFile.PrevSibling as IEqualityExpression;

            var possibleLiteral = context.NodeInFile.Parent;
            if (possibleLiteral is ICSharpLiteralExpression literalExpression)
            {
                stringLiteral = literalExpression;
                eqExpression = possibleLiteral.Parent as IEqualityExpression;
            }

            if (eqExpression != null)
            {
                return (eqExpression.LeftOperand as IReferenceExpression).IsTagProperty() ||
                       (eqExpression.RightOperand as IReferenceExpression).IsTagProperty();
            }

            stringLiteral = null;
            return false;
        }
        
        
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
                return new MatchingResult(matchingResult.MatchedIndices, matchingResult.AdjustedScore - 100, matchingResult.OriginalScore);
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