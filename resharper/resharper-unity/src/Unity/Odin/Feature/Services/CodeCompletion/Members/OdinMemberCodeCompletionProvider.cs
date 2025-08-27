using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Collections;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.References.Members;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.CodeCompletion.Members;

[Language(typeof(CSharpLanguage))]
public class OdinMemberCodeCompletionProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
{
    protected override AutoAcceptBehaviour GetAutocompletionBehaviour(CSharpCodeCompletionContext specificContext)
    {
        return AutoAcceptBehaviour.AutoAcceptWithReplace;
    }
    
    protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
    {
        var odinMemberReference = context.UnterminatedContext.TreeNode?.Parent.GetReferences()
            .OfType<OdinMemberReference>()
            .FirstOrDefault(t => t.GetTreeTextRange().Contains(new TreeOffset(context.BasicContext.CaretTreeOffset - context.TerminatedContext.RootNode.GetTreeStartOffsetThroughSandbox())));

        if (odinMemberReference == null)
            return false;
        
        var symbolTable = odinMemberReference.GetCompletionSymbolTable();
        bool hasAnything = false;

        var rootOffset = context.TerminatedContext.RootNode.GetTreeStartOffsetThroughSandbox();
        var startOffset = odinMemberReference.GetTreeTextRange().StartOffset + rootOffset - 1;
        var endOffset = odinMemberReference.GetTreeTextRange().EndOffset + rootOffset - SyntheticComments.CodeCompletionIdentifierToken.Length;
        var document = context.NodeInFile.GetDocumentRange().Document;
        
        var replaceRange = new DocumentRange(new DocumentOffset(document, startOffset.Offset), new DocumentOffset(document, endOffset.Offset));
        var textLookupRanges = new TextLookupRanges(replaceRange, replaceRange);


        var elements = new Dictionary<string, IDeclaredElement>();
        symbolTable.ForAllSymbolInfos(v =>
        {
            hasAnything = true;
            elements[v.ShortName] = v.GetDeclaredElement();
           
        });

        foreach (var (shortName, element) in elements)
        {
            if (element is ICompiledElement)
                continue;
            
            if (element is IAccessor)
                continue;
            
            if (element is IMethod || element is IField || element is IProperty)
            {
                collector.Add(new StringLiteralItem("$" + shortName, textLookupRanges));
            }
        }

        return hasAnything;
    }
    
    private sealed class StringLiteralItem : TextLookupItemBase, IMLSortingAwareItem
    {
        public StringLiteralItem([NotNull] string text, TextLookupRanges ranges)
        {
            Text = text;
            Ranges = ranges;
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

        public override void Accept(ITextControl textControl, DocumentRange nameRange, LookupItemInsertType insertType,
            Suffix suffix, ISolution solution, bool keepCaretStill)
        {
            base.Accept(textControl, nameRange, LookupItemInsertType.Replace, suffix, solution, keepCaretStill);
        }
        
        protected override void OnAfterComplete(ITextControl textControl, ref DocumentRange nameRange,
            ref DocumentRange decorationRange, TailType tailType, ref Suffix suffix, ref IRangeMarker caretPositionRangeMarker)
        {
            base.OnAfterComplete(textControl, ref nameRange, ref decorationRange, tailType, ref suffix,
                ref caretPositionRangeMarker);
            
            // Consistently move caret to end of path; i.e., end of the string literal, before closing quote
            textControl.Caret.MoveTo(Ranges!.ReplaceRange.StartOffset + Text.Length - 1,
                CaretVisualPlacement.DontScrollIfVisible);
        }

        public bool UseMLSort() => false;
    }

}