#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Resources;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;
using JetBrains.Util.Collections;
using JetBrains.Util.Logging;
using ProjectExtensions = JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.ProjectExtensions;


namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion;

    public abstract class AssetPathCompletionProviderBase : CSharpItemsProviderBase<CSharpCodeCompletionContext>
    {
        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            var stringLiteral = context.StringLiteral();
            if (stringLiteral == null)
                return false;


            var isUnityProject = context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion
                                 && context.PsiModule is IProjectPsiModule projectPsiModule
                                 && projectPsiModule.Project.IsUnityProject();


            return isUnityProject && IsAvailableInCurrentContext(context, stringLiteral);
        }

        public abstract bool IsAvailableInCurrentContext(CSharpCodeCompletionContext context, ICSharpLiteralExpression literalExpression);

        protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
        {
            var stringLiteral = context.StringLiteral();
            if (stringLiteral == null)
                return false;

            var completionSearchInfo = CollectSearchInfo(context, stringLiteral);

            if (completionSearchInfo.AbsolutePathCompletionFolder == null)
                return false;

            switch (completionSearchInfo.SearchPathType)
            {
                case CompletionSearchInfo.PassType.ProjectRoot:
                    AddPredefinedDirectories(collector, completionSearchInfo.Ranges);
                    return true;
                case CompletionSearchInfo.PassType.PackagesRootFolder:
                    AddPackagesLookupItems(collector, completionSearchInfo);
                    return true;
                case CompletionSearchInfo.PassType.InternalFolder:
                    AddResourcesFromAssetsFolder(collector, completionSearchInfo);
                    return true;
                case CompletionSearchInfo.PassType.Unknown:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private CompletionSearchInfo CollectSearchInfo(CSharpCodeCompletionContext context,
            ICSharpLiteralExpression stringLiteral)
        {
            var nodeInFile = context.NodeInFile;
            var solution = nodeInFile.GetSolution();
            var unitySolutionPath = solution.SolutionDirectory;

            var factory = solution.TryGetComponent<UnityExternalFilesModuleFactory>();
            if (factory == null)
                return CompletionSearchInfo.InvalidData;

            var assetIndexingEnabled = solution.GetComponent<AssetIndexingSupport>().IsEnabled.Value;

            IFileSystemWrapper unityExternalFilesPsiModule = assetIndexingEnabled
                ? new UnityPsiModuleWrapper(factory.PsiModule)
                : new FileSystemModuleWrapper();

            var relativeSearchPath = CalculateSearchPath(context, stringLiteral, nodeInFile, out var textLookupRanges);

            if (relativeSearchPath.IsEmpty)
                return new CompletionSearchInfo(unitySolutionPath, CompletionSearchInfo.PassType.ProjectRoot,
                    textLookupRanges, solution, unityExternalFilesPsiModule);


            //Search path like: "Assets/Foo/.."
            return CalculateCompletionSearchInfo(relativeSearchPath, unitySolutionPath, unityExternalFilesPsiModule, textLookupRanges, solution);
        }

        private static CompletionSearchInfo CalculateCompletionSearchInfo(RelativePath relativeSearchPath,
            VirtualFileSystemPath unitySolutionPath, IFileSystemWrapper unityExternalFilesPsiModule,
            TextLookupRanges textLookupRanges, ISolution solution)
        {
            var firstDirectory = relativeSearchPath.Components.FirstOrEmpty;
            if (firstDirectory.EqualTo(ProjectExtensions.AssetsFolder))
            {
                var searchPath = unitySolutionPath.Combine(relativeSearchPath);
                return GetAssetFolderCompletionSearchInfo(searchPath, unitySolutionPath, unityExternalFilesPsiModule, textLookupRanges, solution);
            }

            if (firstDirectory.EqualTo(ProjectExtensions.PackagesFolder))
            {
                var packagesFolderPath = // F:/someFolders/MyProject/Packages
                    unitySolutionPath.Combine(ProjectExtensions.PackagesFolder);

                var packageData = TryGetPackageDataBySearchPath(relativeSearchPath, solution);
                //not valid package - returns packages folder
                if (packageData is null || packageData.PackageFolder == null)
                    return new CompletionSearchInfo(packagesFolderPath,
                        CompletionSearchInfo.PassType.PackagesRootFolder, textLookupRanges, solution,
                        unityExternalFilesPsiModule);

                //extracting path relative to package folder -> FolderA
                var innerPackagePath = relativeSearchPath.RemoveFirstComponent().RemoveFirstComponent();

                // path would be  F:/someFolders/MyProject/Library/PackagesCache/com.companyname.packName/FolderA/
                // or another local directory, or even local Packages folder
                var absolutePathCompletionFolder = packageData.PackageFolder.Combine(innerPackagePath);

                return new CompletionSearchInfo(absolutePathCompletionFolder,
                    CompletionSearchInfo.PassType.InternalFolder, textLookupRanges, solution,
                    unityExternalFilesPsiModule);
            }

            return new CompletionSearchInfo(unitySolutionPath, CompletionSearchInfo.PassType.ProjectRoot,
                textLookupRanges, solution, unityExternalFilesPsiModule);
        }

        private static CompletionSearchInfo GetAssetFolderCompletionSearchInfo(VirtualFileSystemPath searchPath,
            VirtualFileSystemPath unitySolutionPath, IFileSystemWrapper unityExternalFilesPsiModule,
            TextLookupRanges textLookupRanges, ISolution solution)
        {
            //clear wrong not existing part of the path
            while (!unityExternalFilesPsiModule.DirectoryExists(searchPath) &&
                   !searchPath.Equals(unitySolutionPath))
                searchPath = searchPath.Parent;

            return new CompletionSearchInfo(searchPath, CompletionSearchInfo.PassType.InternalFolder,
                textLookupRanges, solution, unityExternalFilesPsiModule);
        }

        protected virtual RelativePath CalculateSearchPath(CSharpCodeCompletionContext context,
            ICSharpLiteralExpression stringLiteral, ITreeNode nodeInFile, out TextLookupRanges textLookupRanges)
        {
            var originalInput = stringLiteral.ConstantValue.AsString() ?? string.Empty;
            originalInput = originalInput.RemoveQuotes() ?? string.Empty;


            var treeEndOffset = nodeInFile.GetTreeEndOffset();

            var firstLetterOffset = nodeInFile.GetTreeStartOffset() + 1; //first symbol is '"' - moving to the next one
            var symbolAfterCaretOffset = context.BasicContext.CaretTreeOffset;
            var symbolBeforeCaretOffset = symbolAfterCaretOffset - 1;

            var lastSymbolIndexBeforeCaret = Math.Max(0, symbolBeforeCaretOffset - firstLetterOffset);
            var lastSlashIndex = originalInput.LastIndexOf('/', lastSymbolIndexBeforeCaret);

            var substringLength = lastSlashIndex >= 0 ? lastSlashIndex + 1 : 0;

            var rootPath = originalInput[..substringLength];
            var relativeSearchPath = RelativePath.Parse(rootPath);

            var lastSlashOffset = firstLetterOffset + lastSlashIndex;

            var contextCompletionRanges = context.CompletionRanges;
            var originalInsertRange = contextCompletionRanges.InsertRange;

            var insertTextRange = new TextRange(lastSlashOffset.Offset + 1, treeEndOffset.Offset);
            var insertRange = new DocumentRange(originalInsertRange.Document, insertTextRange);

            textLookupRanges = new TextLookupRanges(insertRange, insertRange);
            return relativeSearchPath;
        }

        private static PackageData? TryGetPackageDataBySearchPath(RelativePath searchPath, ISolution solution)
        {
            var packageId = ExtractPackageIdFromSearchPath(searchPath);
            if (string.IsNullOrEmpty(packageId))
                return null;

            var packageManager = solution.GetComponent<PackageManager>();
            var packageData = packageManager.GetPackageById(packageId);
            return packageData;
        }


        private static string ExtractPackageIdFromSearchPath(RelativePath searchPath)
        {
            //path = "Packages/com.companyname.packName/FolderA/
            return searchPath.RemoveFirstComponent().Components.FirstOrEmpty.ToString();
        }


        private static void AddPredefinedDirectories(IItemsCollector collector,
            TextLookupRanges contextCompletionRanges)
        {
            collector.Add(new ResourcePathItem(ProjectExtensions.AssetsFolder, true,
                contextCompletionRanges));
            collector.Add(new ResourcePathItem(ProjectExtensions.PackagesFolder, true,
                contextCompletionRanges));
        }

        private static void AddPackagesLookupItems(IItemsCollector collector, CompletionSearchInfo searchInfo)
        {
            var solution = searchInfo.Solution;

            if (solution == null)
                return;

            var packageManager = solution.GetComponent<PackageManager>();
            foreach (var (key, _) in packageManager.Packages)
            {
                var resourcePathItem = new ResourcePathItem(key, true, searchInfo.Ranges);
                collector.Add(resourcePathItem);
            }
        }

        private static void AddResourcesFromAssetsFolder(IItemsCollector collector,
            CompletionSearchInfo completionSearchInfo)
        {
            var solution = completionSearchInfo.Solution;

            var factory = solution?.TryGetComponent<UnityExternalFilesModuleFactory>();
            if (factory == null)
                return;

            var absolutePathCompletionFolder = completionSearchInfo.AbsolutePathCompletionFolder;
            if (absolutePathCompletionFolder == null)
                return;

            var children = completionSearchInfo.FileSystemWrapper.GetChildFilesFolder(absolutePathCompletionFolder);

            foreach (var sourceFilePath in children)
            {
                if (sourceFilePath == null)
                    continue;

                if (!sourceFilePath.IsMeta())
                    continue;

                var nameWithoutExtension = sourceFilePath.NameWithoutExtension;

                try
                {
                    var childAbsolutePath = sourceFilePath.Parent.Combine(nameWithoutExtension);
                    var resourcePathItem = new ResourcePathItem(nameWithoutExtension, childAbsolutePath.ExistsDirectory,
                        completionSearchInfo.Ranges);
                    collector.Add(resourcePathItem);
                }
                catch (Exception e)
                {
                    Logger.LogExceptionSilently(e);
                }
            }
        }

        private readonly struct CompletionSearchInfo
        {
            public enum PassType
            {
                Unknown,
                ProjectRoot,
                PackagesRootFolder,
                InternalFolder,
            }

            public static readonly CompletionSearchInfo InvalidData = new(null, PassType.Unknown,
                new TextLookupRanges(DocumentRange.InvalidRange, DocumentRange.InvalidRange), null, new FileSystemWrapperDummy());

            public readonly TextLookupRanges Ranges;
            public readonly ISolution? Solution;
            public readonly VirtualFileSystemPath? AbsolutePathCompletionFolder;
            public readonly PassType SearchPathType;

            public CompletionSearchInfo(VirtualFileSystemPath? absolutePathCompletionFolder, PassType passType,
                TextLookupRanges ranges, ISolution? solution, IFileSystemWrapper fileSystemWrapper)
            {
                SearchPathType = passType;
                AbsolutePathCompletionFolder = absolutePathCompletionFolder;
                Ranges = ranges;
                Solution = solution;
                FileSystemWrapper = fileSystemWrapper;
            }

            public IFileSystemWrapper FileSystemWrapper { get; }

            public override string ToString()
            {
                return
                    $"{nameof(AbsolutePathCompletionFolder)}: {AbsolutePathCompletionFolder}, {nameof(Ranges)}: {Ranges}, {nameof(SearchPathType)}: {SearchPathType}";
            }
        }

        protected interface IFileSystemWrapper
        {
            bool DirectoryExists(VirtualFileSystemPath path);
            IEnumerable<VirtualFileSystemPath> GetChildFilesFolder(VirtualFileSystemPath path);
        }

        private class UnityPsiModuleWrapper : IFileSystemWrapper
        {
            private readonly UnityExternalFilesPsiModule myModule;

            public UnityPsiModuleWrapper(UnityExternalFilesPsiModule module)
            {
                myModule = module;
            }

            public bool DirectoryExists(VirtualFileSystemPath path)
            {
                return myModule.ContainsPath(path);
            }

            public IEnumerable<VirtualFileSystemPath> GetChildFilesFolder(VirtualFileSystemPath path)
            {
                try
                {
                    return myModule.GetChildFilesFolder(path).Select(sf => sf.GetLocation());
                }
                catch (Exception e)
                {
                    Logger.LogExceptionSilently(e);
                }

                return EnumerableCollection<VirtualFileSystemPath>.Empty;
            }
        }

        private class FileSystemModuleWrapper : IFileSystemWrapper
        {
            public bool DirectoryExists(VirtualFileSystemPath path)
            {
                return path.Exists == FileSystemPath.Existence.Directory;
            }

            public IEnumerable<VirtualFileSystemPath> GetChildFilesFolder(VirtualFileSystemPath path)
            {
                try
                {
                    var virtualFileSystemPaths = path.GetDirectoryEntries();
                    return virtualFileSystemPaths.Select(dirData => dirData.GetAbsolutePath());
                }
                catch (Exception e)
                {
                    Logger.LogExceptionSilently(e);
                }

                return EnumerableCollection<VirtualFileSystemPath>.Empty;
            }
        }

        private class FileSystemWrapperDummy : IFileSystemWrapper
        {
            public bool DirectoryExists(VirtualFileSystemPath path)
            {
                return path.Exists == FileSystemPath.Existence.Directory;
            }

            public IEnumerable<VirtualFileSystemPath> GetChildFilesFolder(VirtualFileSystemPath path)
            {
                return EnumerableCollection<VirtualFileSystemPath>.Empty;
            }
        }

        private sealed class ResourcePathItem : TextLookupItemBase
        {
            private readonly string myCompletionItemName;

            public ResourcePathItem(string completionItemName, bool isDirectory, TextLookupRanges ranges)
            {
                myCompletionItemName = completionItemName;
                Ranges = ranges;
                Text = $"{completionItemName}\"";
                Image = isDirectory
                    ? ProjectModelThemedIcons.Directory.Id
                    : UnityFileTypeThemedIcons.FileUnity.Id;
            }

            protected override RichText GetDisplayName()
            {
                return LookupUtil.FormatLookupString(myCompletionItemName, TextColor);
            }

            public override IconId Image { get; }

            protected override void OnAfterComplete(ITextControl textControl, ref DocumentRange nameRange,
                ref DocumentRange decorationRange,
                TailType tailType, ref Suffix suffix, ref IRangeMarker caretPositionRangeMarker)
            {
                base.OnAfterComplete(textControl, ref nameRange, ref decorationRange, tailType, ref suffix,
                    ref caretPositionRangeMarker);
                // Consistently move caret to end of path; i.e., end of the string literal, before closing quote
                textControl.Caret.MoveTo(Ranges!.ReplaceRange.StartOffset + Text.Length - 1,
                    CaretVisualPlacement.DontScrollIfVisible);
            }

            public override void Accept(ITextControl textControl, DocumentRange nameRange,
                LookupItemInsertType insertType,
                Suffix suffix, ISolution solution, bool keepCaretStill)
            {
                // Force replace + keep caret still in order to place caret at consistent position (see override of OnAfterComplete)
                base.Accept(textControl, nameRange, LookupItemInsertType.Replace, suffix, solution, true);
            }
        }
    }
