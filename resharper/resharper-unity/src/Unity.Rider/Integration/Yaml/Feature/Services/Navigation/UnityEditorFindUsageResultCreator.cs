using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.ProjectsHost.SolutionHost.Progress;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Impl.Search.Operations;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Yaml.Feature.Services.Navigation
{
    [SolutionComponent]
    public class UnityEditorFindUsageResultCreator
    {
        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly ISearchDomain myYamlSearchDomain;
        private readonly IShellLocks myLocks;
        private readonly AssetHierarchyProcessor myAssetHierarchyProcessor;
        private readonly AnimatorScriptUsagesElementContainer myAnimatorContainer;
        private readonly PackageManager myPackageManager;
        private readonly ILogger myLogger;
        private readonly BackendUnityHost myBackendUnityHost;
        private readonly BackgroundProgressManager? myBackgroundProgressManager;
        private readonly FrontendBackendHost myFrontendBackendHost;
        private readonly IPersistentIndexManager myPersistentIndexManager;
        private readonly VirtualFileSystemPath mySolutionDirectoryPath;

        public UnityEditorFindUsageResultCreator(Lifetime lifetime, ISolution solution,
                                                 SearchDomainFactory searchDomainFactory, IShellLocks locks,
                                                 AssetHierarchyProcessor assetHierarchyProcessor,
                                                 BackendUnityHost backendUnityHost,
                                                 FrontendBackendHost frontendBackendHost,
                                                 UnityExternalFilesModuleFactory externalFilesModuleFactory,
                                                 IPersistentIndexManager persistentIndexManager,
                                                 AnimatorScriptUsagesElementContainer animatorContainer,
                                                 PackageManager packageManager,
                                                 ILogger logger,
                                                 BackgroundProgressManager? backgroundProgressManager = null)
        {
            myLifetime = lifetime;
            mySolution = solution;
            myLocks = locks;
            myAssetHierarchyProcessor = assetHierarchyProcessor;
            myBackendUnityHost = backendUnityHost;
            myBackgroundProgressManager = backgroundProgressManager;
            myYamlSearchDomain = searchDomainFactory.CreateSearchDomain(externalFilesModuleFactory.PsiModule);
            myFrontendBackendHost = frontendBackendHost;
            myAnimatorContainer = animatorContainer;
            myPackageManager = packageManager;
            myLogger = logger;
            myPersistentIndexManager = persistentIndexManager;
            mySolutionDirectoryPath = solution.SolutionDirectory;
        }

        public void CreateRequestToUnity(IDeclaredElement declaredElement, LocalReference location)
        {
            var finder = mySolution.GetPsiServices().AsyncFinder;
            var consumer = new UnityUsagesFinderConsumer(myPackageManager, myLogger, myAssetHierarchyProcessor,
                myAnimatorContainer, myPersistentIndexManager,
                mySolutionDirectoryPath, declaredElement);

            var sourceFile = myPersistentIndexManager[location.OwningPsiPersistentIndex];
            if (sourceFile == null)
                return;

            var selectRequest = CreateRequest(myPackageManager, myLogger, mySolutionDirectoryPath,
                myAssetHierarchyProcessor, myAnimatorContainer,
                location, sourceFile, declaredElement);
            if (selectRequest == null)
                return;

            var requestLifetimeDefinition = myLifetime.CreateNested();
            var pi = new ProgressIndicator(myLifetime);
            if (myBackgroundProgressManager != null)
            {
                var task = BackgroundProgressBuilder.Create()
                    .WithTitle(Strings.UnityEditorFindUsageResultCreator_CreateRequestToUnity_Finding_usages_in_Unity_for__ + declaredElement.ShortName)
                    .AsIndeterminate()
                    .AsCancelable(() => { pi.Cancel(); })
                    .Build();

                myBackgroundProgressManager.AddNewTask(requestLifetimeDefinition.Lifetime, task);
            }

            myLocks.Tasks.StartNew(myLifetime, Scheduling.MainGuard, () =>
            {
                using (ReadLockCookie.Create())
                {
                    finder.FindAsync(new[] { declaredElement }, myYamlSearchDomain, consumer, SearchPattern.FIND_USAGES,
                        pi, FinderSearchRoot.Empty,
                        new UnityUsagesAsyncFinderCallback(myLifetime, requestLifetimeDefinition, consumer, myFrontendBackendHost,
                            myBackendUnityHost, myLocks, declaredElement.ShortName, selectRequest));
                }
            });
        }

        private static AssetFindUsagesResultBase? CreateRequest(PackageManager packageManager, ILogger logger,
            VirtualFileSystemPath solutionDirPath,
            AssetHierarchyProcessor assetDocumentHierarchy,
            AnimatorScriptUsagesElementContainer animatorContainer,
            LocalReference location, IPsiSourceFile sourceFile,
            IDeclaredElement declaredElement,
            bool needExpand = false)
        {
            if (!GetPathFromAssetFolder(packageManager, logger, solutionDirPath, sourceFile, out var pathFromAsset, out var fileName, out var extension))
                return null;

            var path = sourceFile.GetLocation();
            if (path.IsController() &&
                animatorContainer.GetElementsNames(location, declaredElement, out var names, out var isStateMachine) &&
                names != null)
            {
                return new AnimatorFindUsagesResult(names,
                    isStateMachine ? AnimatorUsageType.StateMachine : AnimatorUsageType.State, needExpand,
                    pathFromAsset, fileName, extension);
            }

            if (path.IsAsset())
            {
                return new AssetFindUsagesResult(needExpand, pathFromAsset, fileName, extension);
            }

            if (path.IsAnim())
            {
                return new AnimationFindUsagesResult(needExpand, pathFromAsset, fileName, extension);
            }

            var consumer = new UnityScenePathGameObjectConsumer();
            assetDocumentHierarchy.ProcessSceneHierarchyFromComponentToRoot(location, consumer, true, true);

            return new HierarchyFindUsagesResult(consumer.NameParts.ToArray(), consumer.RootIndexes.ToArray(), needExpand, pathFromAsset, fileName, extension);
        }

        private static bool GetPathFromAssetFolder(PackageManager packageManager,
            ILogger logger,
            VirtualFileSystemPath solutionDirPath,
            IPsiSourceFile file,
            [NotNullWhen(true)] out string? filePath,
            [NotNullWhen(true)] out string? fileName,
            [NotNullWhen(true)] out string? extension)
        {
            extension = null;
            filePath = null;
            fileName = null;

            var path = file.GetLocation().MakeRelativeTo(solutionDirPath);
            var pathComponents = path.Components;
            var assetFolder = pathComponents.FirstOrEmpty;
            if (assetFolder.Equals(UnityYamlConstants.AssetsFolder))
            {
                filePath = string.Join("/", pathComponents.Select(t => t.ToString()));
            }
            else
            {
                var packageData = packageManager.GetOwningPackage(file.GetLocation());
                if (packageData == null || packageData.PackageFolder == null)
                {
                    var ex = new Assertion.AssertionException(packageData == null
                        ? "Failed to determine Package for path."
                        : "PackageFolder is null for absolute path.");
                    ex.AddSensitiveData("Path", file.GetLocation());
                    logger.Error(ex);
                    return false;
                }
                var pathInsidePackage = file.GetLocation().MakeRelativeTo(packageData.PackageFolder);
                filePath = $"Packages/{packageData.Id}/{pathInsidePackage}";
            }

            extension = path.ExtensionWithDot;
            fileName = path.NameWithoutExtension;

            return true;
        }

        private class UnityUsagesFinderConsumer : IFindResultConsumer<UnityAssetFindResult>
        {
            private readonly PackageManager myPackageManager;
            private readonly ILogger myLogger;
            private readonly AssetHierarchyProcessor myAssetHierarchyProcessor;
            private readonly AnimatorScriptUsagesElementContainer myAnimatorContainer;
            private readonly IPersistentIndexManager myPersistentIndexManager;
            private readonly VirtualFileSystemPath mySolutionDirectoryPath;
            private readonly IDeclaredElement myDeclaredElement;

            public readonly List<AssetFindUsagesResultBase> Result = new();

            public UnityUsagesFinderConsumer(PackageManager packageManager, ILogger logger,
                                             AssetHierarchyProcessor assetHierarchyProcessor,
                                             AnimatorScriptUsagesElementContainer animatorContainer,
                                             IPersistentIndexManager persistentIndexManager,
                                             VirtualFileSystemPath solutionDirectoryPath,
                                             IDeclaredElement declaredElement)
            {
                myPackageManager = packageManager;
                myLogger = logger;
                myAssetHierarchyProcessor = assetHierarchyProcessor;
                myPersistentIndexManager = persistentIndexManager;
                mySolutionDirectoryPath = solutionDirectoryPath;
                myAnimatorContainer = animatorContainer;
                myDeclaredElement = declaredElement;
            }

            public UnityAssetFindResult Build(FindResult result)
            {
                // IFindResultConsumer<T> doesn't mark T Build(FindResult) as allowing to return null
                return (result as UnityAssetFindResult)!;
            }

            public FindExecution Merge(UnityAssetFindResult data)
            {
                var sourceFile = myPersistentIndexManager[data.OwningElementLocation.OwningPsiPersistentIndex];
                if (sourceFile == null)
                    return FindExecution.Continue;

                var request = CreateRequest(myPackageManager, myLogger, mySolutionDirectoryPath, myAssetHierarchyProcessor, myAnimatorContainer,
                    data.OwningElementLocation, sourceFile, myDeclaredElement);
                if (request != null)
                    Result.Add(request);

                return FindExecution.Continue;
            }
        }

        private class UnityUsagesAsyncFinderCallback : IFinderAsyncCallback
        {
            private readonly LifetimeDefinition myProgressBarLifetimeDefinition;
            private readonly Lifetime myComponentLifetime;
            private readonly UnityUsagesFinderConsumer myConsumer;
            private readonly FrontendBackendHost myFrontendBackendHost;
            private readonly BackendUnityHost myBackendUnityHost;
            private readonly IShellLocks myShellLocks;
            private readonly string myDisplayName;
            private readonly AssetFindUsagesResultBase mySelected;

            public UnityUsagesAsyncFinderCallback(Lifetime componentLifetime,
                                                  LifetimeDefinition progressBarLifetimeDefinition,
                                                  UnityUsagesFinderConsumer consumer,
                                                  FrontendBackendHost frontendBackendHost,
                                                  BackendUnityHost backendUnityHost,
                                                  IShellLocks shellLocks,
                                                  string displayName,
                                                  AssetFindUsagesResultBase selected)
            {
                myProgressBarLifetimeDefinition = progressBarLifetimeDefinition;
                myComponentLifetime = componentLifetime;
                myConsumer = consumer;
                myFrontendBackendHost = frontendBackendHost;
                myBackendUnityHost = backendUnityHost;
                myShellLocks = shellLocks;
                myDisplayName = displayName;
                mySelected = selected;
            }

            public void Complete()
            {
                myShellLocks.Tasks.StartNew(myComponentLifetime, Scheduling.MainGuard, () =>
                {
                    if (myConsumer.Result.Count != 0)
                    {
                        if (myBackendUnityHost.BackendUnityModel.Value == null) return;

                        myFrontendBackendHost.Do(a => a.AllowSetForegroundWindow
                            .Start(myProgressBarLifetimeDefinition.Lifetime, Unit.Instance).Result
                            .AdviseOnce(myComponentLifetime,
                                _ =>
                                {
                                    var model = myBackendUnityHost.BackendUnityModel.Value;
                                    model.ShowUsagesInUnity.Fire(mySelected);
                                    // pass all references to Unity TODO temp workaround, replace with async api
                                    model.SendFindUsagesSessionResult.Fire(
                                        new FindUsagesSessionResult(myDisplayName, myConsumer.Result.ToArray()));
                                }));
                    }

                    myProgressBarLifetimeDefinition.Terminate();
                });
            }

            public void Error(string message)
            {
                myProgressBarLifetimeDefinition.Terminate();
            }
        }
    }
}
