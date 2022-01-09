using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.RdBackend.Common.Features.BackgroundTasks;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
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
using JetBrains.Rider.Backend.Features.BackgroundTasks;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Yaml.Feature.Services.Navigation
{
    [SolutionComponent]
    public class UnityEditorFindUsageResultCreator
    {
        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly ISearchDomain myYamlSearchDomain;
        private readonly IShellLocks myLocks;
        private readonly AssetHierarchyProcessor myAssetHierarchyProcessor;
        [NotNull] private readonly AnimatorScriptUsagesElementContainer myAnimatorContainer;
        private readonly BackendUnityHost myBackendUnityHost;
        private readonly RiderBackgroundTaskHost myBackgroundTaskHost;
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
                                                 [NotNull] AnimatorScriptUsagesElementContainer animatorContainer,
                                                 [CanBeNull] RiderBackgroundTaskHost backgroundTaskHost = null)
        {
            myLifetime = lifetime;
            mySolution = solution;
            myLocks = locks;
            myAssetHierarchyProcessor = assetHierarchyProcessor;
            myBackendUnityHost = backendUnityHost;
            myBackgroundTaskHost = backgroundTaskHost;
            myYamlSearchDomain = searchDomainFactory.CreateSearchDomain(externalFilesModuleFactory.PsiModule);
            myFrontendBackendHost = frontendBackendHost;
            myAnimatorContainer = animatorContainer;
            myPersistentIndexManager = persistentIndexManager;
            mySolutionDirectoryPath = solution.SolutionDirectory;
        }

        public void CreateRequestToUnity([NotNull] IDeclaredElement declaredElement, LocalReference location, bool focusUnity)
        {
            var finder = mySolution.GetPsiServices().AsyncFinder;
            var consumer = new UnityUsagesFinderConsumer(myAssetHierarchyProcessor, myAnimatorContainer, myPersistentIndexManager,
                mySolutionDirectoryPath, declaredElement);

            var sourceFile = myPersistentIndexManager[location.OwningPsiPersistentIndex];
            if (sourceFile == null)
                return;

            var selectRequest = CreateRequest(mySolutionDirectoryPath, myAssetHierarchyProcessor, myAnimatorContainer,
                location, sourceFile, declaredElement, false);


            var lifetimeDef = myLifetime.CreateNested();
            var pi = new ProgressIndicator(myLifetime);
            if (myBackgroundTaskHost != null)
            {
                var task = RiderBackgroundTaskBuilder.Create()
                    .WithTitle("Finding usages in Unity for: " + declaredElement.ShortName)
                    .AsIndeterminate()
                    .AsCancelable(() => { pi.Cancel(); })
                    .Build();

                myBackgroundTaskHost.AddNewTask(lifetimeDef.Lifetime, task);
            }

            myLocks.Tasks.StartNew(myLifetime, Scheduling.MainGuard, () =>
            {
                using (ReadLockCookie.Create())
                {
                    finder.FindAsync(new[] {declaredElement}, myYamlSearchDomain,
                        consumer, SearchPattern.FIND_USAGES ,pi,
                        FinderSearchRoot.Empty, new UnityUsagesAsyncFinderCallback(lifetimeDef, myLifetime, consumer, myFrontendBackendHost, myBackendUnityHost, myLocks,
                            declaredElement.ShortName, selectRequest, focusUnity));
                }
            });
        }

        private static AssetFindUsagesResultBase CreateRequest(VirtualFileSystemPath solutionDirPath,
                                                               AssetHierarchyProcessor assetDocumentHierarchy,
                                                               [NotNull]
                                                               AnimatorScriptUsagesElementContainer animatorContainer,
                                                               LocalReference location, IPsiSourceFile sourceFile,
                                                               [NotNull] IDeclaredElement declaredElement,
                                                               bool needExpand = false)
        {
            if (!GetPathFromAssetFolder(solutionDirPath, sourceFile, out var pathFromAsset, out var fileName, out var extension))
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

        private static bool GetPathFromAssetFolder([NotNull] VirtualFileSystemPath solutionDirPath, [NotNull] IPsiSourceFile file,
            out string filePath, out string fileName, out string extension)
        {
            extension = null;
            filePath = null;
            fileName = null;
            var path = file.GetLocation().MakeRelativeTo(solutionDirPath);
            var assetFolder = path.Components.FirstOrEmpty;
            if (!assetFolder.Equals(UnityYamlConstants.AssetsFolder))
                return false;

            var pathComponents = path.Components;

            extension = path.ExtensionWithDot;
            fileName = path.NameWithoutExtension;
            filePath =  String.Join("/", pathComponents.Select(t => t.ToString()));

            return true;
        }

        private class UnityUsagesFinderConsumer : IFindResultConsumer<UnityAssetFindResult>
        {
            private readonly AssetHierarchyProcessor myAssetHierarchyProcessor;
            [NotNull] private readonly AnimatorScriptUsagesElementContainer myAnimatorContainer;
            private readonly IPersistentIndexManager myPersistentIndexManager;
            private readonly VirtualFileSystemPath mySolutionDirectoryPath;
            private FindExecution myFindExecution = FindExecution.Continue;
            [NotNull] private readonly IDeclaredElement myDeclaredElement;

            public List<AssetFindUsagesResultBase> Result = new List<AssetFindUsagesResultBase>();

            public UnityUsagesFinderConsumer(AssetHierarchyProcessor assetHierarchyProcessor,
                                             [NotNull] AnimatorScriptUsagesElementContainer animatorContainer,
                                             IPersistentIndexManager persistentIndexManager,
                                             VirtualFileSystemPath solutionDirectoryPath,
                                             [NotNull] IDeclaredElement declaredElement)
            {
                myAssetHierarchyProcessor = assetHierarchyProcessor;
                myPersistentIndexManager = persistentIndexManager;
                mySolutionDirectoryPath = solutionDirectoryPath;
                myAnimatorContainer = animatorContainer;
                myDeclaredElement = declaredElement;
            }

            public UnityAssetFindResult Build(FindResult result)
            {
                return result as UnityAssetFindResult;
            }

            public FindExecution Merge(UnityAssetFindResult data)
            {
                var sourceFile = myPersistentIndexManager[data.OwningElemetLocation.OwningPsiPersistentIndex];
                if (sourceFile == null)
                    return myFindExecution;

                var request = CreateRequest(mySolutionDirectoryPath, myAssetHierarchyProcessor, myAnimatorContainer,
                    data.OwningElemetLocation, sourceFile, myDeclaredElement);
                if (request != null)
                    Result.Add(request);

                return myFindExecution;
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

            public UnityUsagesAsyncFinderCallback(LifetimeDefinition progressBarLifetimeDefinition,
                                                  Lifetime componentLifetime, UnityUsagesFinderConsumer consumer,
                                                  FrontendBackendHost frontendBackendHost,
                                                  BackendUnityHost backendUnityHost, IShellLocks shellLocks,
                                                  string displayName, AssetFindUsagesResultBase selected,
                                                  bool focusUnity)
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

                        myFrontendBackendHost.Do(a => a.AllowSetForegroundWindow.Start(Unit.Instance).Result
                            .AdviseOnce(myComponentLifetime,
                                result =>
                                {
                                    var model = myBackendUnityHost.BackendUnityModel.Value;
                                    if (mySelected != null)
                                        model.ShowUsagesInUnity.Fire(mySelected);
                                    // pass all references to Unity TODO temp workaround, replace with async api
                                    model.SendFindUsagesSessionResult.Fire(new FindUsagesSessionResult(myDisplayName, myConsumer.Result.ToArray()));
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