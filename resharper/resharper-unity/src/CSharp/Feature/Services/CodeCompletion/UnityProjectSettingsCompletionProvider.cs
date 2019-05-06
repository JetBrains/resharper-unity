using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;
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
            var ranges = context.CompletionRanges;
            IEnumerable<string> completionItems = null;
            ICSharpLiteralExpression argumentLiteral = null;
            
            // scene completion
            if (IsSpecificArgumentInSpecificMethod(context, out argumentLiteral, IsLoadSceneMethod, IsCorrespondingArgument("sceneName")))
            {
                var cache = context.NodeInFile.GetSolution().TryGetComponent<UnityProjectSettingsCache>();
                completionItems = cache.GetAllPossibleSceneNames();

            } // tag completion, tag == "..."
            else if (IsTagEquality(context, out argumentLiteral)) 
            {
                var cache = context.NodeInFile.GetSolution().TryGetComponent<UnityProjectSettingsCache>();
                completionItems = cache.GetAllTags();
            } //// tag completion, CompareTag("...")
            else if (IsSpecificArgumentInSpecificMethod(context, out argumentLiteral, IsCompareTagMethod , IsCorrespondingArgument("tag")))
            {
                var cache = context.NodeInFile.GetSolution().TryGetComponent<UnityProjectSettingsCache>();
                completionItems = cache.GetAllTags();
            } // layer completion
            else if (IsSpecificArgumentInSpecificMethod(context, out argumentLiteral, IsLayerMaskNameToLayer,
                IsCorrespondingArgument("layerName")) || IsSpecificArgumentInSpecificMethod(context, out argumentLiteral, IsLayerMaskGetMask,
                           (_, __) => true))
            {
                var cache = context.NodeInFile.GetSolution().TryGetComponent<UnityProjectSettingsCache>();
                completionItems = cache.GetAllLayers();
            }  // input completion
            else if (IsSpecificArgumentInSpecificMethod(context, out argumentLiteral, IsInputButtonMethod, IsCorrespondingArgument("buttonName")) || 
                     IsSpecificArgumentInSpecificMethod(context, out argumentLiteral, IsInputAxisMethod, IsCorrespondingArgument("axisName")))
            {
                var cache = context.NodeInFile.GetSolution().TryGetComponent<UnityProjectSettingsCache>();
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

        private bool IsLayerMaskGetMask(IInvocationExpression expr)
        {
            return IsSpecificMethod(expr, KnownTypes.LayerMask, "GetMask");
        }

        private bool IsLayerMaskNameToLayer(IInvocationExpression expr)
        {
            return IsSpecificMethod(expr, KnownTypes.LayerMask, "NameToLayer");
        }

        private bool IsCompareTagMethod(IInvocationExpression expr)
        {
            return IsSpecificMethod(expr, KnownTypes.Component, "CompareTag");
        }

        private static readonly string[] ourInputButtonNames = {"GetButtonDown", "GetButtonUp", "GetButton"}; 
        private static readonly string[] ourInputAxisNames = {"GetAxis", "GetAxisRaw"}; 
        private bool IsInputButtonMethod(IInvocationExpression invocationExpression)
        {
            return IsSpecificMethod(invocationExpression, KnownTypes.Input, ourInputButtonNames);
        }
        
        private static bool IsInputAxisMethod(IInvocationExpression invocationExpression)
        {
            return IsSpecificMethod(invocationExpression, KnownTypes.Input, ourInputAxisNames);
        }
        
        private static bool IsSpecificMethod(IInvocationExpression invocationExpression, IClrTypeName typeName, params string[] methodNames)
        {
            var declaredElement = invocationExpression.Reference?.Resolve().DeclaredElement as IMethod;
            if (declaredElement == null)
                return false;


            if (methodNames.Any(t => t.Equals(declaredElement.ShortName)))
            {
                return declaredElement.GetContainingType()?.GetClrName().Equals(typeName) == true;
            } 
            return false;
        }

        private bool IsSpecificArgumentInSpecificMethod(CSharpCodeCompletionContext context, out ICSharpLiteralExpression stringLiteral, 
            Func<IInvocationExpression, bool> methodChecker, Func<IArgumentList, ICSharpArgument, bool> argumentChecker)
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

                if (argumentChecker(argumentList, argument))
                {
                    stringLiteral = literalExpression;
                    possibleInvocationExpression = InvocationExpressionNavigator.GetByArgument(argument);
                } 
            }
            
            
            if (possibleInvocationExpression is IInvocationExpression invocationExpression)
            {
                if (methodChecker(invocationExpression))
                {
                    return true;
                }
            }

            stringLiteral = null;
            return false;
        }

        private Func<IArgumentList, ICSharpArgument, bool> IsCorrespondingArgument(string argumentName)
        {
            return (argumentList, argument) => argument.IsNamedArgument && argument.NameIdentifier.Name.Equals(argumentName) ||
                   !argument.IsNamedArgument && argumentList.Arguments[0] == argument;
        }
        
        private bool IsLoadSceneMethod(IInvocationExpression invocationExpression)
        {
            return IsSceneManagerSceneRelatedMethod(invocationExpression.InvocationExpressionReference) ||
                   IsEditorSceneManagerSceneRelatedMethod(invocationExpression.InvocationExpressionReference);
        }

        private bool IsTagEquality(CSharpCodeCompletionContext context, out ICSharpLiteralExpression stringLiteral)
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
                return CompareTagProblemAnalyzer.IsTagReference(eqExpression.LeftOperand as IReferenceExpression) ||
                       CompareTagProblemAnalyzer.IsTagReference(eqExpression.RightOperand as IReferenceExpression);
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