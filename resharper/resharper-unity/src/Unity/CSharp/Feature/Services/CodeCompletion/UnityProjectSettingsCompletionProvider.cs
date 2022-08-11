using System.Collections.Generic;
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
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages;
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
    public class UnityProjectSettingsCompletionProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
    {
        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
        }

        protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
        {
            var ranges = context.CompletionRanges;
            IEnumerable<string> completionItems = null;

            // scene completion
            if (UnityCompletionUtils.IsSpecificArgumentInSpecificMethod(context, out var argumentLiteral, IsLoadSceneMethod, UnityCompletionUtils.IsCorrespondingArgument("sceneName")))
            {
                var cache = context.NodeInFile.GetSolution().GetComponent<UnityProjectSettingsCache>();
                completionItems = cache.GetAllPossibleSceneNames();

            } // animator state completion
            else if (UnityCompletionUtils.IsSpecificArgumentInSpecificMethod(context, out argumentLiteral, IsPlayAnimationMethod, UnityCompletionUtils.IsCorrespondingArgument("stateName")))
            {
                var container = context.NodeInFile.GetSolution().GetComponent<AnimatorScriptUsagesElementContainer>();
                completionItems = container.GetStateNames();
            } // layer completion
            else if (UnityCompletionUtils.IsSpecificArgumentInSpecificMethod(context, out argumentLiteral, ExpressionReferenceUtils.IsLayerMaskNameToLayerMethod, UnityCompletionUtils.IsCorrespondingArgument("layerName")) || 
                     UnityCompletionUtils.IsSpecificArgumentInSpecificMethod(context, out argumentLiteral, ExpressionReferenceUtils.IsLayerMaskGetMaskMethod,
                           (_, __) => true))
            {
                var cache = context.NodeInFile.GetSolution().GetComponent<UnityProjectSettingsCache>();
                completionItems = cache.GetAllLayers();
            }  // input completion
            else if (UnityCompletionUtils.IsSpecificArgumentInSpecificMethod(context, out argumentLiteral, ExpressionReferenceUtils.IsInputButtonMethod, UnityCompletionUtils.IsCorrespondingArgument("buttonName")) ||
                     UnityCompletionUtils.IsSpecificArgumentInSpecificMethod(context, out argumentLiteral, ExpressionReferenceUtils.IsInputAxisMethod, UnityCompletionUtils.IsCorrespondingArgument("axisName")))
            {
                var cache = context.NodeInFile.GetSolution().GetComponent<UnityProjectSettingsCache>();
                completionItems = cache.GetAllInput();
            }

            var any = false;

            if (argumentLiteral != null)
            {
                var offset = argumentLiteral.GetDocumentRange().EndOffset;
                ranges = ranges.WithInsertRange(ranges.InsertRange.SetEndTo(offset)).WithReplaceRange(ranges.ReplaceRange.SetEndTo(offset));
            }

            if (completionItems != null)
            {
                foreach (var sceneName in completionItems)
                {
                    any = true;
                    var item = new StringLiteralItem($"\"{sceneName}\"");
                    item.InitializeRanges(ranges, context.BasicContext);
                    collector.Add(item);
                }
            }

            return any;
        }

        private bool IsLoadSceneMethod(IInvocationExpression invocationExpression)
        {
            return invocationExpression.InvocationExpressionReference.IsSceneManagerSceneRelatedMethod() ||
                   invocationExpression.InvocationExpressionReference.IsEditorSceneManagerSceneRelatedMethod();
        }
        
        private static bool IsPlayAnimationMethod([NotNull] IInvocationExpression invocationExpression)
        {
            return invocationExpression.InvocationExpressionReference.IsAnimatorPlayMethod();
        }

        private sealed class StringLiteralItem : TextLookupItemBase
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
        }
    }
}