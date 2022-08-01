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
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Resx.Utils;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Icons;

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
            if (!UnityCompletionUtils.IsSpecificArgumentInSpecificMethod(context, out var argumentLiteral,
                    IsResourcesLoadMethod,
                    UnityCompletionUtils.IsCorrespondingArgument("path")))
                return false;


            var resourceLoadCache = context.NodeInFile.GetSolution().GetComponent<ResourceLoadCache>();
            var packageManager = context.NodeInFile.GetSolution().GetComponent<PackageManager>();


            var any = false;
            foreach (var assetsFolderResource in resourceLoadCache.CachedResources)
            {
                var completionRelativePath = assetsFolderResource.RelativePath.ConvertToString()?.Replace("\\", "/");
                var locationDescription = "Assets/";

                if (assetsFolderResource.ResourceLocationType is ResourceLocationType.PackageEditor
                    or ResourceLocationType.PackagePlayer)
                {
                    var packageData = packageManager.GetPackageByAssetPath(assetsFolderResource.VirtualFileSystemPath);
                    if (packageData == null)
                        continue;

                    locationDescription = packageData.PackageDetails.DisplayName;
                }

                var item = new ResourcesCompletionItem(completionRelativePath,
                    locationDescription,
                    assetsFolderResource.ResourceLocationType,
                    assetsFolderResource.Extension);

                item.InitializeRanges(context.CompletionRanges, context.BasicContext);
                collector.Add(item);
                any = true;
            }

            return any;
        }

        private bool IsResourcesLoadMethod(IInvocationExpression invocationExpression)
        {
            return invocationExpression.InvocationExpressionReference.IsResourcesLoadMethod();
        }

        private sealed class ResourcesCompletionItem : TextLookupItemBase
        {
            public ResourcesCompletionItem([NotNull] string text, [NotNull] string additionalInfo,
                ResourceLocationType locationType,
                string extension)
            {
                Text = $"\"{text}\"";
                DisplayTypeName = $"{additionalInfo}({locationType})";
                LookupUtil.AddInformationText(DisplayName, extension);
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

            public override void Accept(
                ITextControl textControl, DocumentRange nameRange, LookupItemInsertType insertType,
                Suffix suffix, ISolution solution, bool keepCaretStill)
            {
                base.Accept(textControl, nameRange, LookupItemInsertType.Replace, suffix, solution, keepCaretStill);
            }
        }
    }
}