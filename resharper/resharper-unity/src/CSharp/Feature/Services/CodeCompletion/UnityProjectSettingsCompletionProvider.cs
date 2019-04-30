using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util.Literals;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using static JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityProjectSettingsUtils;

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
            if (IsSceneManagerLoadSceneStringArgument(context, out var currentStringLiteral))
            {
                var ranges = context.CompletionRanges;

                if (currentStringLiteral != null)
                {
                    var offset = currentStringLiteral.GetDocumentRange().EndOffset;
                    ranges = ranges.WithInsertRange(ranges.InsertRange.SetEndTo(offset)).WithReplaceRange(ranges.ReplaceRange.SetEndTo(offset));
                }

                var any = false;    
                var cache = context.NodeInFile.GetSolution().TryGetComponent<UnityProjectSettingsCache>();
                if (cache != null)
                {
                    foreach (var sceneName in cache.GetAllPossibleSceneNames())
                    {
                        any = true;
                        var item = new StringLiteralItem($"\"{sceneName}\"");
                        item.InitializeRanges(ranges, context.BasicContext);
                        collector.Add(item);
                    }
                }

                return any;
            }
            return false;
        }

        private bool IsSceneManagerLoadSceneStringArgument(CSharpCodeCompletionContext context, out ICSharpLiteralExpression stringLiteral)
        {
            stringLiteral = null;
            var nodeInFile = context.NodeInFile as ITokenNode;
            if (nodeInFile == null)
                return false;

            var possibleInvocationExpression = nodeInFile.Parent;
            if (possibleInvocationExpression is ICSharpLiteralExpression literalExpression)
            {
                if (!literalExpression.Literal.IsAnyStringLiteral())
                    return false;
                
                var argument = CSharpArgumentNavigator.GetByValue(literalExpression);
                var argumentList = ArgumentListNavigator.GetByArgument(argument);
                if (argument == null || argumentList == null)
                    return false;

                if (argument.IsNamedArgument && argument.NameIdentifier.Name.Equals("sceneName") || 
                    !argument.IsNamedArgument && argumentList.Arguments[0] == argument)
                {
                    stringLiteral = literalExpression;
                    possibleInvocationExpression = InvocationExpressionNavigator.GetByArgument(argument);
                } 
            }
            
            if (possibleInvocationExpression is IInvocationExpression invocationExpression)
            {
                if (IsSceneManagerSceneRelatedMethod(invocationExpression.InvocationExpressionReference) ||
                    IsEditorSceneManagerSceneRelatedMethod(invocationExpression.InvocationExpressionReference))
                {
                    return true;
                }
            }

            stringLiteral = null;
            return false;
        }
        
        
        private sealed class StringLiteralItem : TextLookupItemBase
        {
            public StringLiteralItem([NotNull] string text)
            {
                Text = text;
            }

            public override IconId Image => PsiSymbolsThemedIcons.Const.Id;

            public override MatchingResult Match(PrefixMatcher prefixMatcher,ITextControl textControl)
            {
                var matchingResult = prefixMatcher.Matcher(Text);
                if (matchingResult == null)
                    return null;
                return new MatchingResult(matchingResult.MatchedIndices, matchingResult.MostLikelyContinuation, matchingResult.AdjustedScore - 100, matchingResult.OriginalScore);
            }
        }
    }
}