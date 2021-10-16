using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Feature.CodeCompletion;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.TestRunner.Abstractions.Extensions;

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
            var directives = preProcessingDirectiveCache.GetPreProcessingDirectives(assemblyName);

            var textRange = literal.GetInnerTreeTextRange();
            var replaceRange = context.UnterminatedContext.ToDocumentRange(textRange);
            var caretOffset = context.BasicContext.CaretDocumentOffset;
            var insertRange = replaceRange.Contains(caretOffset) ? replaceRange.SetEndTo(caretOffset) : replaceRange;
            var textLookupRanges = new TextLookupRanges(insertRange, replaceRange);
            var visualReplaceRangeMarker = textLookupRanges.CreateVisualReplaceRangeMarker();

            foreach (var directive in directives)
            {
                var item = new TextLookupItem(directive.Name, PsiSymbolsThemedIcons.Const.Id);
                item.InitializeRanges(textLookupRanges, context.BasicContext);
                item.VisualReplaceRangeMarker = visualReplaceRangeMarker;

                collector.Add(item);
            }

            return true;
        }
    }
}