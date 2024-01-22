using System.Linq;
using System.Text;
using JetBrains.Annotations;
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
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.CodeCompletion;

[Language(typeof(CSharpLanguage))]
public class OdinLayoutAttributesCodeCompletionProvider  : CSharpItemsProviderBase<CSharpCodeCompletionContext>
{
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
        var stringLiteral = context.StringLiteral();
        if (stringLiteral == null)
            return false;

        if (context.NodeInFile.GetContainingNode<IArgument>() == null)
            return false;

        var isUnityProject = context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion
                             && context.PsiModule is IProjectPsiModule projectPsiModule
                             && projectPsiModule.Project.IsUnityProject();

        return isUnityProject;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
    {
        var stringLiteral = context.StringLiteral();
        if (stringLiteral == null)
            return false;

        var nodeInFile = context.NodeInFile;

        var argument = nodeInFile.GetContainingNode<ICSharpArgument>();
        var attribute = AttributeNavigator.GetByArgument(argument);
        
        if (attribute == null)
            return false;

        var declaration = attribute.GetContainingNode<IClassLikeDeclaration>();

        var classLikeDeclaredElement = declaration?.DeclaredElement;
        if (classLikeDeclaredElement == null)
            return false;
        
        var type = attribute.TypeReference?.Resolve().Result.DeclaredElement as ITypeElement;
        if (type == null)
            return false;

        if (!OdinAttributeUtil.IsLayoutAttribute(type))
            return false;
        
        var literal = stringLiteral.ConstantValue.AsString();
        if (literal == null)
            return false;

        var name = argument.MatchingParameter?.Element.ShortName;
        if (name == null)
            return false;

        if (!OdinKnownAttributes.LayoutAttributesParameterKinds.TryGetValue((type.GetClrName(), name), out var kind))
            return false;

        var caretInLiteralOffset = (context.BasicContext.CaretTreeOffset - stringLiteral.GetTreeStartOffset()) - 1; // -1 for '"' char

        var layoutNames = classLikeDeclaredElement.GetMembers()
            .SelectMany(t => t.GetAttributeInstances(AttributesSource.Self))
            .Where(t => OdinAttributeUtil.IsLayoutAttribute(t.GetAttributeType().GetTypeElement()))
            .SelectNotNull(t => OdinAttributeUtil.GetLayoutName(t, kind));
        
        var trie = new QualifiedNamesTrie<string>(false, '/');

        foreach (var layoutName in layoutNames)
        {
            var sections = layoutName.Split('/');
            var sb = new StringBuilder(layoutName.Length);
            foreach (var section in sections)
            {
                sb.Append(section);
                trie.Add(sb.ToString(), section);
                sb.Append('/');
            }
        }
        
        var prefixBeforeCaret = literal.Substring(0, caretInLiteralOffset);
        var lastSectionStartIndex = prefixBeforeCaret.LastIndexOf('/');
        var node = trie.FindTrieNode(lastSectionStartIndex >= 0 ? prefixBeforeCaret.Substring(0, lastSectionStartIndex) : prefixBeforeCaret);
        if (node == null)
            return false;
        
        var insertRange = new DocumentRange(stringLiteral.GetDocumentStartOffset() + 1 + lastSectionStartIndex + 1, stringLiteral.GetDocumentEndOffset() - 1);
        foreach (var child in node.Children)
        {
            if (child.Data.IsEmpty())
                continue;
         
            collector.Add(new StringLiteralItem(child.Data, new TextLookupRanges(insertRange, insertRange)));
        }

        return true;
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