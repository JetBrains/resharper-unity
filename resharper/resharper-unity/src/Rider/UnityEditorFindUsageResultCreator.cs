using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Core;
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
        private readonly UnityEditorProtocol myEditorProtocol;
        private readonly FileSystemPath mySolutionDirectoryPath;

        public UnityEditorFindUsageResultCreator(Lifetime lifetime, ISolution solution, SearchDomainFactory searchDomainFactory, IShellLocks locks,
            UnitySceneDataLocalCache sceneDataCache, UnityHost unityHost, UnityExternalFilesModuleFactory externalFilesModuleFactory,
            UnityEditorProtocol editorProtocol,
            [CanBeNull] RiderBackgroundTaskHost backgroundTaskHost = null)
        {
            myLifetime = lifetime;
            mySolution = solution;
            myLocks = locks;
            myUnitySceneDataLocalCache = sceneDataCache;
            myBackgroundTaskHost = backgroundTaskHost;
            myYamlSearchDomain = searchDomainFactory.CreateSearchDomain(externalFilesModuleFactory.PsiModule);
            myUnityHost = unityHost;
            myEditorProtocol = editorProtocol;
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
                        FinderSearchRoot.Empty, new UnityUsagesAsyncFinderCallback(lifetimeDef, myLifetime, consumer, myUnityHost, myEditorProtocol, myLocks,
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

        public static void CreateRequestAndShow([NotNull]  UnityEditorProtocol editor, UnityHost host, Lifetime lifetime, [NotNull] FileSystemPath solutionDirPath, [NotNull]UnitySceneDataLocalCache unitySceneDataLocalCache, 
            [NotNull] string anchor, IPsiSourceFile sourceFile, bool needExpand = false)
        {
            FindUsageResultElement request;
            using (ReadLockCookie.Create())
            {
                request = CreateRequest(solutionDirPath, unitySceneDataLocalCache, anchor, sourceFile, needExpand);
            }
            
            host.PerformModelAction(a => a.AllowSetForegroundWindow.Start(Unit.Instance).Result.Advise(lifetime,
                result =>
                {
                    editor.UnityModel.Value.ShowGameObjectOnScene.Fire(request.ConvertToUnityModel());
                }));
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
                if (sourceFile == null || !sourceFile.IsValid())
                    return myFindExecution;
                
                var anchor = UnitySceneDataUtil.GetAnchorFromBuffer(data.ComponentDocument.GetTextAsBuffer());
                if (anchor == null)
                    return myFindExecution;
                
                var request = CreateRequest(mySolutionDirectoryPath, myUnitySceneDataLocalCache, anchor, sourceFile);
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
            private readonly FindUsageResultElement mySelected;

            public UnityUsagesAsyncFinderCallback(LifetimeDefinition progressBarLifetimeDefinition, Lifetime componentLifetime, UnityUsagesFinderConsumer consumer, UnityHost unityHost, UnityEditorProtocol editorProtocol, IShellLocks shellLocks, 
                string displayName, FindUsageResultElement selected, bool focusUnity)
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
                                        model.ShowGameObjectOnScene.Fire(mySelected.ConvertToUnityModel());
                                    // pass all references to Unity TODO temp workaround, replace with async api
                                    model.FindUsageResults.Fire(new FindUsageResult(myDisplayName,
                                        myConsumer.Result.ToArray()).ConvertToUnityModel());
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