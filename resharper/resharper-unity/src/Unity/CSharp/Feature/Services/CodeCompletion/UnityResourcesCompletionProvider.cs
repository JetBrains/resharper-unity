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
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion
{
    [Language(typeof(CSharpLanguage))]
    public class UnityResourcesCompletionProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
    {
        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
        }

        protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
        {
            if (!UnityCompletionUtils.IsSpecificArgumentInSpecificMethod(context, out _,
                    ExpressionReferenceUtils.IsResourcesLoadMethod,
                    UnityCompletionUtils.IsCorrespondingArgument("path")))
                return false;


            var resourceLoadCache = context.NodeInFile.GetSolution().GetComponent<ResourceLoadCache>();
            var packageManager = context.NodeInFile.GetSolution().GetComponent<PackageManager>();


            bool CollectAutocompletion(ResourceLoadCache.ResourceCacheInfo assetsFolderResource)
            {
                var locationDescription = "Assets/";

                if (assetsFolderResource.ResourceLocationType is ResourceLocationType.PackageEditor
                    or ResourceLocationType.PackagePlayer)
                {
                    var packageData = packageManager.GetPackageByAssetPath(assetsFolderResource.VirtualFileSystemPath);
                    if (packageData == null)
                        return  false;

                    locationDescription = packageData.PackageDetails.DisplayName;
                }

                var completionRelativePath =
                    assetsFolderResource.RelativePath.NormalizeSeparators(FileSystemPathEx.SeparatorStyle.Unix);

                var item = new ResourcesCompletionItem(completionRelativePath,
                    locationDescription,
                    assetsFolderResource.ResourceLocationType,
                    assetsFolderResource.Extension);

                item.InitializeRanges(context.CompletionRanges, context.BasicContext);
                collector.Add(item);
                
                return true;
            }


            return resourceLoadCache.CollectItems(CollectAutocompletion);
        }

        private sealed class ResourcesCompletionItem : TextLookupItemBase, IMLSortingAwareItem
        {
            public ResourcesCompletionItem([NotNull] string text, [NotNull] string additionalInfo,
                ResourceLocationType locationType,
                string extension)
            {
                Text = $"\"{text}\"";
                DisplayTypeName = $"{additionalInfo}({locationType})";
                LookupUtil.AddInformationText(DisplayName, extension);
            }

            public override IconId Image => UnityFileTypeThemedIcons.FileUnityAsset.Id;

            public override MatchingResult Match(PrefixMatcher prefixMatcher)
            {
                var matchingResult = prefixMatcher.Match(Text);
                if (matchingResult == null)
                    return null;
                return new MatchingResult(matchingResult.MatchedIndices, matchingResult.AdjustedScore - 100,
                    matchingResult.OriginalScore);
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