using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Plugins.Json.Feature.CodeCompletion;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.Text;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.CodeCompletion
{
    [Language(typeof(JsonNewLanguage))]
    public class AsmDefDefineConstraintItemsProvider
        : ItemsProviderOfSpecificContext<JsonNewCodeCompletionContext>
    {
        protected override bool IsAvailable(JsonNewCodeCompletionContext context)
        {
            if (context.BasicContext.CodeCompletionType != CodeCompletionType.BasicCompletion
                && context.BasicContext.CodeCompletionType != CodeCompletionType.SmartCompletion)
            {
                return false;
            }

            var literal = context.UnterminatedContext.TreeNode?.GetContainingNode<IJsonNewLiteralExpression>();
            return literal?.IsDefineConstraintsArrayEntry() == true;
        }

        protected override TextLookupRanges GetDefaultRanges(JsonNewCodeCompletionContext context) => context.Ranges;

        protected override bool AddLookupItems(JsonNewCodeCompletionContext context, IItemsCollector collector)
        {
            var file = (context.BasicContext.File as IJsonNewFile).NotNull("context.BasicContext.File as IJsonNewFile != null")!;
            var literal = context.UnterminatedContext.TreeNode?.GetContainingNode<IJsonNewLiteralExpression>();
            if (literal == null)
                return false;

            var assemblyName = file.GetRootObject().GetFirstPropertyValue<IJsonNewLiteralExpression>("name")?.GetStringValue();
            if (assemblyName == null || string.IsNullOrWhiteSpace(assemblyName))
                return false;

            var preProcessingDirectiveCache = context.BasicContext.Solution.GetComponent<PreProcessingDirectiveCache>();
            var directives = preProcessingDirectiveCache.GetAllPreProcessingDirectives(assemblyName);

            var textRange = literal.GetInnerTreeTextRange();
            var buffer = literal.GetTextAsBuffer();
            var caretBufferOffset = context.BasicContext.CaretTreeOffset - literal.GetTreeStartOffset();

            // Update range to only modify the current define symbol - delimited by trailing space or end of string, and
            // by leading space, start of string or the ! symbol
            var endOfWord = buffer.IndexOf(" ", caretBufferOffset);
            var startOfWord = buffer.LastIndexOfAny(new[] { ' ', '!' }, caretBufferOffset);
            if (endOfWord != -1)
                textRange = textRange.SetEndTo(textRange.StartOffset + endOfWord - 1);
            if (startOfWord != -1)
                textRange = textRange.SetStartTo(textRange.StartOffset + startOfWord);

            var replaceRange = context.UnterminatedContext.ToDocumentRange(textRange);
            var caretDocumentOffset = context.BasicContext.CaretDocumentOffset;
            var insertRange = replaceRange.Contains(caretDocumentOffset) ? replaceRange.SetEndTo(caretDocumentOffset) : replaceRange;
            var textLookupRanges = new TextLookupRanges(insertRange, replaceRange);
            var visualReplaceRangeMarker = textLookupRanges.CreateVisualReplaceRangeMarker();

            var names = directives.Directives.Select(d => d.Name)
                .Concat(directives.InvalidDirectives.Select(d => d.Name))
                .Distinct(IdentityFunc<string>.Instance)
                .OrderBy(IdentityFunc<string>.Instance);

            foreach (var directive in names)
            {
                var item = new TextLookupItem(directive, PsiSymbolsThemedIcons.Const.Id);
                item.InitializeRanges(textLookupRanges, context.BasicContext);
                item.VisualReplaceRangeMarker = visualReplaceRangeMarker;

                collector.Add(item);
            }

            return true;
        }
    }
}