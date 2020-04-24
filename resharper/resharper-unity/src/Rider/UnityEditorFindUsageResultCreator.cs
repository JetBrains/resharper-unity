using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Core;
using JetBrains.Lifetimes;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.BackgroundTasks;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Impl.Search.Operations;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityEditorFindUsageResultCreator
    {
        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly ISearchDomain myYamlSearchDomain;
        private readonly IShellLocks myLocks;
        private readonly AssetHierarchyProcessor myAssetHierarchyProcessor;
        private readonly RiderBackgroundTaskHost myBackgroundTaskHost;
        private readonly UnityHost myUnityHost;
        private readonly UnityEditorProtocol myEditorProtocol;
        private readonly IPersistentIndexManager myPersistentIndexManager;
        private readonly FileSystemPath mySolutionDirectoryPath;

        public UnityEditorFindUsageResultCreator(Lifetime lifetime, ISolution solution, SearchDomainFactory searchDomainFactory, IShellLocks locks,
            AssetHierarchyProcessor assetHierarchyProcessor, UnityHost unityHost, UnityExternalFilesModuleFactory externalFilesModuleFactory,
            UnityEditorProtocol editorProtocol, IPersistentIndexManager persistentIndexManager,
            [CanBeNull] RiderBackgroundTaskHost backgroundTaskHost = null)
        {
            myLifetime = lifetime;
            mySolution = solution;
            myLocks = locks;
            myAssetHierarchyProcessor = assetHierarchyProcessor;
            myBackgroundTaskHost = backgroundTaskHost;
            myYamlSearchDomain = searchDomainFactory.CreateSearchDomain(externalFilesModuleFactory.PsiModule);
            myUnityHost = unityHost;
            myEditorProtocol = editorProtocol;
            myPersistentIndexManager = persistentIndexManager;
            mySolutionDirectoryPath = solution.SolutionDirectory;
        }

        public void CreateRequestToUnity([NotNull] IDeclaredElement declaredElement, LocalReference location, bool focusUnity)
        {
            var finder = mySolution.GetPsiServices().AsyncFinder;
            var consumer = new UnityUsagesFinderConsumer(myAssetHierarchyProcessor, myPersistentIndexManager, mySolutionDirectoryPath);

            var sourceFile = myPersistentIndexManager[location.OwnerId];
            if (sourceFile == null)
                return;
            
            var selectRequest = CreateRequest(mySolutionDirectoryPath, myAssetHierarchyProcessor, location, sourceFile, false);
            
            
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
                        FinderSearchRoot.Empty, new UnityUsagesAsyncFinderCallback(lifetimeDef, myLifetime, consumer, myUnityHost, myEditorProtocol, myLocks,
                            declaredElement.ShortName, selectRequest, focusUnity));
                }
            });
        }

        private static AssetFindUsagesResultBase CreateRequest(FileSystemPath solutionDirPath, AssetHierarchyProcessor assetDocumentHierarchy, 
            LocalReference location, IPsiSourceFile sourceFile, bool needExpand = false)
        {
            if (!GetPathFromAssetFolder(solutionDirPath, sourceFile, out var pathFromAsset, out var fileName, out var extension))
                return null;

            if (sourceFile.GetLocation().ExtensionWithDot.EndsWith(UnityYamlFileExtensions.AssetFileExtensionWithDot))
            {
                return new AssetFindUsagesResult(needExpand, pathFromAsset, fileName, extension);
            }

            var consumer = new UnityScenePathGameObjectConsumer();
            assetDocumentHierarchy.ProcessSceneHierarchyFromComponentToRoot(location, consumer, true, true);
            
            return new HierarchyFindUsagesResult(consumer.NameParts.ToArray(), consumer.RootIndexes.ToArray(), needExpand, pathFromAsset, fileName, extension);
        }

        private static bool GetPathFromAssetFolder([NotNull] FileSystemPath solutionDirPath, [NotNull] IPsiSourceFile file, 
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
            private readonly IPersistentIndexManager myPersistentIndexManager;
            private readonly FileSystemPath mySolutionDirectoryPath;
            private FindExecution myFindExecution = FindExecution.Continue;
            
            public List<AssetFindUsagesResultBase> Result = new List<AssetFindUsagesResultBase>();

            public UnityUsagesFinderConsumer(AssetHierarchyProcessor assetHierarchyProcessor, IPersistentIndexManager persistentIndexManager,
                FileSystemPath solutionDirectoryPath)
            {
                myAssetHierarchyProcessor = assetHierarchyProcessor;
                myPersistentIndexManager = persistentIndexManager;
                mySolutionDirectoryPath = solutionDirectoryPath;
            }
            
            public UnityAssetFindResult Build(FindResult result)
            {
                return result as UnityAssetFindResult;
            }

            public FindExecution Merge(UnityAssetFindResult data)
            {
                var sourceFile = myPersistentIndexManager[data.AttachedElement.Location.OwnerId];
                if (sourceFile == null)
                    return myFindExecution;
                
                var request = CreateRequest(mySolutionDirectoryPath, myAssetHierarchyProcessor, data.AttachedElement.Location, sourceFile);
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
            private readonly UnityHost myUnityHost;
            private readonly UnityEditorProtocol myEditorProtocol;
            private readonly IShellLocks myShellLocks;
            private readonly string myDisplayName;
            private readonly AssetFindUsagesResultBase mySelected;

            public UnityUsagesAsyncFinderCallback(LifetimeDefinition progressBarLifetimeDefinition, Lifetime componentLifetime, UnityUsagesFinderConsumer consumer, UnityHost unityHost, UnityEditorProtocol editorProtocol, IShellLocks shellLocks, 
                string displayName, AssetFindUsagesResultBase selected, bool focusUnity)
            {
                myProgressBarLifetimeDefinition = progressBarLifetimeDefinition;
                myComponentLifetime = componentLifetime;
                myConsumer = consumer;
                myUnityHost = unityHost;
                myEditorProtocol = editorProtocol;
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
                        if (myEditorProtocol.UnityModel.Value == null) return;

                        myUnityHost.PerformModelAction(a => a.AllowSetForegroundWindow.Start(Unit.Instance).Result
                            .Advise(myComponentLifetime,
                                result =>
                                {
                                    var model = myEditorProtocol.UnityModel.Value;
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