using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Feature.CodeCompletion;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Packages;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Psi;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.CodeCompletion
{
    [Language(typeof(JsonNewLanguage))]
    public class AsmDefVersionDefinesNameItemsProvider
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
            return literal?.IsVersionDefinesObjectNameValue() == true;
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

            var textRange = literal.GetInnerTreeTextRange();
            var replaceRange = context.UnterminatedContext.ToDocumentRange(textRange);
            var caretOffset = context.BasicContext.CaretDocumentOffset;
            var insertRange = replaceRange.Contains(caretOffset) ? replaceRange.SetEndTo(caretOffset) : replaceRange;
            var textLookupRanges = new TextLookupRanges(insertRange, replaceRange);
            var visualReplaceRangeMarker = textLookupRanges.CreateVisualReplaceRangeMarker();

            // Special "Unity" resource, to allow setting a define based on the version of the app
            // TODO: Proper icon
            var lookupItem = new TextLookupItem("Unity", UnityFileTypeThemedIcons.FileUnity.Id);
            lookupItem.InitializeRanges(textLookupRanges, context.BasicContext);
            lookupItem.VisualReplaceRangeMarker = visualReplaceRangeMarker;
            lookupItem.Placement.Relevance |= (ulong)LookupItemRelevance.HighSelectionPriority;
            lookupItem.Placement.SetSelectionPriority(SelectionPriority.High);
            collector.Add(lookupItem);

            var packageManager = context.BasicContext.Solution.GetComponent<PackageManager>();
            foreach (var (_, packageData) in packageManager.Packages)
            {
                // TODO: Proper icon
                var item = new TextLookupItem(packageData.Id, UnityFileTypeThemedIcons.FolderPackageReferenced.Id);
                item.InitializeRanges(textLookupRanges, context.BasicContext);
                item.VisualReplaceRangeMarker = visualReplaceRangeMarker;

                collector.Add(item);
            }

            return true;
        }
    }
}