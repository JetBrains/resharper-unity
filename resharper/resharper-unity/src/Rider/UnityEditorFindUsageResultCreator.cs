using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.BackgroundTasks;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Search.Operations;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
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
        private readonly UnitySceneDataLocalCache myUnitySceneDataLocalCache;
        private readonly RiderBackgroundTaskHost myBackgroundTaskHost;
        private readonly UnityHost myUnityHost;
        private readonly FileSystemPath mySolutionDirectoryPath;

        public UnityEditorFindUsageResultCreator(Lifetime lifetime, ISolution solution, SearchDomainFactory searchDomainFactory, IShellLocks locks,
            UnitySceneDataCache sceneDataCache, UnityHost unityHost, UnityExternalFilesModuleFactory externalFilesModuleFactory, [CanBeNull] RiderBackgroundTaskHost backgroundTaskHost = null)
        {
            myLifetime = lifetime;
            mySolution = solution;
            myLocks = locks;
            myUnitySceneDataLocalCache = sceneDataCache.UnitySceneDataLocalCache;
            myBackgroundTaskHost = backgroundTaskHost;
            myYamlSearchDomain = searchDomainFactory.CreateSearchDomain(externalFilesModuleFactory.PsiModule);
            myUnityHost = unityHost;
            mySolutionDirectoryPath = solution.SolutionDirectory;
        }

        public void CreateRequestToUnity([NotNull] IUnityYamlReference yamlReference, bool focusUnity)
        {
            var declaredElement = yamlReference.Resolve().DeclaredElement;
            if (declaredElement == null)
                return;

            var sourceFile = yamlReference.ComponentDocument.GetSourceFile();
            if (sourceFile == null)
                return;

            var anchor = UnitySceneDataUtil.GetAnchorFromBuffer(yamlReference.ComponentDocument.GetTextAsBuffer());
            
            CreateRequestToUnity(declaredElement, sourceFile, anchor, focusUnity);
        }

        public void CreateRequestToUnity([NotNull] IDeclaredElement declaredElement, IPsiSourceFile selectedSourceFile, string selectAnchor, bool focusUnity)
        {
            var finder = mySolution.GetPsiServices().AsyncFinder;
            var consumer = new UnityUsagesFinderConsumer(myUnitySceneDataLocalCache, mySolutionDirectoryPath);

            var selectRequest = (selectedSourceFile == null || selectAnchor == null)
                ? null
                : CreateRequest(mySolutionDirectoryPath, myUnitySceneDataLocalCache, selectAnchor, selectedSourceFile, false);
            
            
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
                        FinderSearchRoot.Empty, new UnityUsagesAsyncFinderCallback(lifetimeDef, consumer, myUnityHost, myLocks,
                            declaredElement.ShortName, selectRequest, focusUnity));
                }
            });
        }

        public static FindUsageResultElement CreateRequest([NotNull] FileSystemPath solutionDirPath, [NotNull]UnitySceneDataLocalCache unitySceneDataLocalCache, 
            [NotNull] string anchor, IPsiSourceFile sourceFile, bool needExpand = false)
        {
            if (!GetPathFromAssetFolder(solutionDirPath, sourceFile, out var pathFromAsset, out var fileName, out var extension))
                return null;
            
            bool isPrefab = extension.Equals(UnityYamlConstants.Prefab, StringComparison.OrdinalIgnoreCase);
            
            var consumer = new UnityPathCachedSceneConsumer();
            unitySceneDataLocalCache.ProcessSceneHierarchyFromComponentToRoot(sourceFile, anchor, consumer);
            
            return new FindUsageResultElement(isPrefab, needExpand, pathFromAsset, fileName, consumer.NameParts.ToArray(), consumer.RootIndexes.ToArray());
        }

        public static void CreateRequestAndShow([NotNull]  UnityHost unityHost, [NotNull] FileSystemPath solutionDirPath, [NotNull]UnitySceneDataLocalCache unitySceneDataLocalCache, 
            [NotNull] string anchor, IPsiSourceFile sourceFile, bool needExpand = false)
        {
            var request = CreateRequest(solutionDirPath, unitySceneDataLocalCache, anchor, sourceFile, needExpand);
            unityHost.PerformModelAction(t => t.ShowGameObjectOnScene.Fire(request));
            UnityFocusUtil.FocusUnity(unityHost.GetValue(t => t.UnityProcessId.Value));
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
        
        private class UnityUsagesFinderConsumer : IFindResultConsumer<IUnityYamlReference>
        {
            private readonly UnitySceneDataLocalCache myUnitySceneDataLocalCache;
            private readonly FileSystemPath mySolutionDirectoryPath;
            private FindExecution myFindExecution = FindExecution.Continue;
            
            public List<FindUsageResultElement> Result = new List<FindUsageResultElement>();

            public UnityUsagesFinderConsumer(UnitySceneDataLocalCache unitySceneDataLocalCache, FileSystemPath solutionDirectoryPath)
            {
                myUnitySceneDataLocalCache = unitySceneDataLocalCache;
                mySolutionDirectoryPath = solutionDirectoryPath;
            }
            
            public IUnityYamlReference Build(FindResult result)
            {
                return (result as FindResultReference)?.Reference as IUnityYamlReference;
            }

            public FindExecution Merge(IUnityYamlReference data)
            {
                var sourceFile = data.ComponentDocument.GetSourceFile();
                var anchor = UnitySceneDataUtil.GetAnchorFromBuffer(data.ComponentDocument.GetTextAsBuffer());
                if (anchor == null || sourceFile == null)
                    return myFindExecution;
                
                var request = CreateRequest(mySolutionDirectoryPath, myUnitySceneDataLocalCache, anchor, sourceFile);
                if (request != null)
                    Result.Add(request);
                
                return myFindExecution;
            }

        }
        
        private class UnityUsagesAsyncFinderCallback : IFinderAsyncCallback
        {
            private readonly LifetimeDefinition myLifetimeDef;
            private readonly UnityUsagesFinderConsumer myConsumer;
            private readonly UnityHost myUnityHost;
            private readonly IShellLocks myShellLocks;
            private readonly string myDisplayName;
            private readonly FindUsageResultElement mySelected;
            private readonly bool myFocusUnity;

            public UnityUsagesAsyncFinderCallback(LifetimeDefinition lifetimeDef, UnityUsagesFinderConsumer consumer, UnityHost unityHost, IShellLocks shellLocks, 
                string displayName, FindUsageResultElement selected, bool focusUnity)
            {
                myLifetimeDef = lifetimeDef;
                myConsumer = consumer;
                myUnityHost = unityHost;
                myShellLocks = shellLocks;
                myDisplayName = displayName;
                mySelected = selected;
                myFocusUnity = focusUnity;
            }

            public void Complete()
            {
                myShellLocks.Tasks.StartNew(myLifetimeDef.Lifetime, Scheduling.MainGuard, () =>
                {
                    if (myConsumer.Result.Count != 0)
                    {

                        if (myFocusUnity)
                            UnityFocusUtil.FocusUnity(myUnityHost.GetValue(t => t.UnityProcessId.Value));

                        if (mySelected != null)
                            myUnityHost.PerformModelAction(t => t.ShowGameObjectOnScene.Fire(mySelected));
                        myUnityHost.PerformModelAction(t =>
                            t.FindUsageResults.Fire(new FindUsageResult(myDisplayName, myConsumer.Result.ToArray())));

                    }
    
                    myLifetimeDef.Terminate();
                });
            }

            public void Error(string message)
            {
                myLifetimeDef.Terminate();
            }
        }
    }
}