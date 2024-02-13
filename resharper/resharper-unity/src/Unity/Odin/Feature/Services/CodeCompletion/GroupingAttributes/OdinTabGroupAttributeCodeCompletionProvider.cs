using System.Collections.Generic;
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

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.CodeCompletion.GroupingAttributes;

[Language(typeof(CSharpLanguage))]
public class OdinTabGroupAttributeCodeCompletionProvider  : CSharpItemsProviderBase<CSharpCodeCompletionContext>
{
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
        var solution = context.NodeInFile.GetSolution();
        if (!OdinAttributeUtil.HasOdinSupport(solution))
            return false;
        
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

        if (!type.GetClrName().Equals(OdinKnownAttributes.TabGroupAttribute))
            return false;
        
        var name = argument.MatchingParameter?.Element.ShortName;
        if (name == null)
            return false;

        if (!"tab".Equals(name))
            return false;
        
        var groups = OdinAttributeUtil.CollectGroupInfo(classLikeDeclaredElement).Where(t =>
            Equals(t.AttributeInstance.GetClrName(), OdinKnownAttributes.TabGroupAttribute) && t.IsMajorGroup).ToList();
        
        var currentMembers = CSharpTypeMemberDeclarationNavigator.GetByAttribute(attribute).Select(t => t.DeclaredElement).ToHashSet();
        if (currentMembers.Count == 0)
            return false;

        var currentGroup = groups.First(t => Equals(t.Member, currentMembers.First())).GroupPath;
        var sectionCount = currentGroup.Count(t => t == '/');
        
        var resultTabs = new HashSet<string>(groups.Count);
        foreach (var group in groups)
        {
            if (currentMembers.Contains(group.Member))
                continue;
            
            if (!group.GroupPath.StartsWith(currentGroup))
                continue;

            var paths = group.GroupPath.Split('/');
            if (paths.Length <= sectionCount)
                continue;

            resultTabs.Add(paths[sectionCount]);
        }
        
        var hasResult = false;
        foreach (var layoutName in resultTabs)
        {
            var item = new StringLiteralItem(layoutName, context.CompletionRanges);
            collector.Add(item);
            hasResult = true;
        }
        
        return hasResult;
    }
    
    private sealed class StringLiteralItem : TextLookupItemBase, IMLSortingAwareItem
    {
        public StringLiteralItem([NotNull] string text, TextLookupRanges ranges)
        {
            Text = "\"" + text + "\"";
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