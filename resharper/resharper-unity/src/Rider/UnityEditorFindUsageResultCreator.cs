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
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Search.Operations;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityEditorFindUsageResultCreator
    {
        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly ISearchDomain myYamlSearchDomain;
        private readonly IShellLocks myLocks;
        private readonly RiderBackgroundTaskHost myBackgroundTaskHost;
        private readonly UnitySceneProcessor mySceneProcessor;
        private readonly UnityHost myUnityHost;
        private readonly FileSystemPath mySolutionDirectoryPath;

        public UnityEditorFindUsageResultCreator(Lifetime lifetime, ISolution solution, SearchDomainFactory searchDomainFactory, IShellLocks locks,
            UnitySceneProcessor sceneProcessor, UnityHost unityHost, UnityExternalFilesModuleFactory externalFilesModuleFactory, [CanBeNull] RiderBackgroundTaskHost backgroundTaskHost = null)
        {
            myLifetime = lifetime;
            mySolution = solution;
            myLocks = locks;
            myBackgroundTaskHost = backgroundTaskHost;
            mySceneProcessor = sceneProcessor;
            myYamlSearchDomain = searchDomainFactory.CreateSearchDomain(externalFilesModuleFactory.PsiModule);
            myUnityHost = unityHost;
            mySolutionDirectoryPath = solution.SolutionDirectory;
        }

        public void CreateRequestToUnity([NotNull] IUnityYamlReference yamlReference, bool focusUnity)
        {
            var declaredElement = yamlReference.Resolve().DeclaredElement;
            if (declaredElement == null)
                return;
            
            CreateRequestToUnity(declaredElement, yamlReference, focusUnity);
        }

        public void CreateRequestToUnity([NotNull] IDeclaredElement declaredElement, [CanBeNull] IUnityYamlReference selectedReference, bool focusUnity)
        {
            var finder = mySolution.GetPsiServices().AsyncFinder;
            var consumer = new UnityUsagesFinderConsumer(mySceneProcessor, mySolutionDirectoryPath, selectedReference);

            var selectRequest = selectedReference == null
                ? null
                : CreateRequest(mySolutionDirectoryPath, mySceneProcessor, selectedReference.ComponentDocument, false);

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

        public static FindUsageResultElement CreateRequest([NotNull] FileSystemPath solutionDirPath, [NotNull]UnitySceneProcessor sceneProcessor, 
            [NotNull] IYamlDocument document, bool needExpand = false)
        {
            var sourceFile = document.GetSourceFile();
            if (sourceFile == null)
                return null;

            var pathElements = UnityObjectPsiUtil.GetGameObjectPathFromComponent(sceneProcessor, document).RemoveEnd("\\").Split("\\");
            
            // Constructing path of child indices
            var consumer = new UnityChildPathSceneConsumer();
            sceneProcessor.ProcessSceneHierarchyFromComponentToRoot(document, consumer);


            if (!GetPathFromAssetFolder(solutionDirPath, sourceFile, out var pathFromAsset, out var fileName, out var extension))
                return null;
            bool isPrefab = extension.Equals(UnityYamlConstants.Prefab, StringComparison.OrdinalIgnoreCase);
            
            return new FindUsageResultElement(isPrefab, needExpand, pathFromAsset, fileName, pathElements, consumer.RootIndices.ToArray().Reverse().ToArray());
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
            private readonly UnitySceneProcessor mySceneProcessor;
            private readonly FileSystemPath mySolutionDirectoryPath;
            private readonly IUnityYamlReference mySelectedReference;
            private FindExecution myFindExecution = FindExecution.Continue;
            
            public List<FindUsageResultElement> Result = new List<FindUsageResultElement>();

            public UnityUsagesFinderConsumer(UnitySceneProcessor sceneProcessor, FileSystemPath solutionDirectoryPath, IUnityYamlReference selectedReference)
            {
                mySceneProcessor = sceneProcessor;
                mySolutionDirectoryPath = solutionDirectoryPath;
                mySelectedReference = selectedReference;
            }
            
            public IUnityYamlReference Build(FindResult result)
            {
                return (result as FindResultReference)?.Reference as IUnityYamlReference;
            }

            public FindExecution Merge(IUnityYamlReference data)
            {
                var request = CreateRequest(mySolutionDirectoryPath, mySceneProcessor, data.ComponentDocument, data == mySelectedReference);
                if (request != null)
                    Result.Add(request);
                
                return myFindExecution;
            }

        }
        

        private class UnityChildPathSceneConsumer : IUnitySceneProcessorConsumer
        {
            public readonly List<int> RootIndices = new List<int>();
            
            public bool ConsumeGameObject(IYamlDocument gameObject, IBlockMappingNode modifications)
            {
                int rootOrder = -1;
                var transform = UnityObjectPsiUtil.FindTransformComponentForGameObject(gameObject);
                if (modifications != null)
                {
                    if (!int.TryParse(UnityObjectPsiUtil.GetValueFromModifications(modifications, transform.GetFileId(), UnityYamlConstants.RootOrderProperty), out rootOrder))
                        rootOrder = -1;
                }
                if (rootOrder == -1)
                {
                    var rootOrderAsString = transform.GetUnityObjectPropertyValue(UnityYamlConstants.RootOrderProperty).AsString();
                    if (!int.TryParse(rootOrderAsString, out rootOrder))
                        rootOrder = -1;
                }
                RootIndices.Add(rootOrder);

                return true;
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