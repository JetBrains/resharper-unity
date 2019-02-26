using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Rider.Model;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityEditorFindUsageResultCreator
    {
        private readonly ISolution mySolution;
        private readonly UnitySceneProcessor mySceneProcessor;
        private readonly UnityHost myUnityHost;
        private readonly FileSystemPath mySolutionDirectoryPath;

        public UnityEditorFindUsageResultCreator(ISolution solution, UnitySceneProcessor sceneProcessor, UnityHost unityHost)
        {
            mySolution = solution;
            mySceneProcessor = sceneProcessor;
            myUnityHost = unityHost;
            mySolutionDirectoryPath = solution.SolutionDirectory;
        }

        public bool CreateRequestToUnity([NotNull] IUnityYamlReference yamlReference, bool focusUnity)
        {
            var declaredElement = yamlReference.Resolve().DeclaredElement;
            if (declaredElement == null)
                return false;
            
            return CreateRequestToUnity(declaredElement, yamlReference, focusUnity);
        }

        public bool CreateRequestToUnity([NotNull] IDeclaredElement declaredElement, [CanBeNull] IUnityYamlReference selectedReference, bool focusUnity)
        {
            var finder = mySolution.GetPsiServices().Finder;
            var references = finder.FindAllReferences(declaredElement).OfType<IUnityYamlReference>();

            var result = new List<FindUsageResultElement>();

            foreach (var reference in references)
            {
                var request = CreateRequest(reference, selectedReference);
                if (request != null)
                    result.Add(request);
            }

            if (result.Count == 0)
                return false;

            if (focusUnity)
                UnityFocusUtil.FocusUnity(myUnityHost.GetValue(t => t.UnityProcessId.Value));
            
            if (selectedReference != null)
                myUnityHost.PerformModelAction(t => t.ShowGameObjectOnScene.Fire(CreateRequest(selectedReference, null)));
            myUnityHost.PerformModelAction(t => t.FindUsageResults.Fire(new FindUsageResult(declaredElement.ShortName, result.ToArray())));
            return true;
        }
        
        private FindUsageResultElement CreateRequest([NotNull] IUnityYamlReference currentReference, [CanBeNull] IUnityYamlReference selectedReference)
        {
            var monoScriptDocument = currentReference.ComponentDocument;

            var sourceFile = monoScriptDocument?.GetSourceFile();
            if (sourceFile == null)
                return null;

            var pathElements = UnityObjectPsiUtil.GetGameObjectPathFromComponent(mySceneProcessor, currentReference.ComponentDocument).RemoveEnd("\\").Split("\\");
            
            // Constructing path of child indices
            var consumer = new UnityChildPathSceneConsumer();
            mySceneProcessor.ProcessSceneHierarchyFromComponentToRoot(monoScriptDocument, consumer);


            if (!GetPathFromAssetFolder(sourceFile, out var pathFromAsset, out var fileName, out var extension))
                return null;
            bool needExpand = currentReference == selectedReference;
            bool isPrefab = extension.Equals(UnityYamlConstants.Prefab, StringComparison.OrdinalIgnoreCase);
            
            return new FindUsageResultElement(isPrefab, needExpand, pathFromAsset, fileName, pathElements, consumer.RootIndices.ToArray().Reverse().ToArray());
        }

        private bool GetPathFromAssetFolder([NotNull] IPsiSourceFile file, out string filePath, out string fileName, out string extension)
        {
            extension = null;
            filePath = null;
            fileName = null;
            var path = file.GetLocation().MakeRelativeTo(mySolutionDirectoryPath);
            var assetFolder = path.Components.FirstOrEmpty;
            if (!assetFolder.Equals(UnityYamlConstants.AssetsFolder)) 
                return false;
            
            var pathComponents = path.Components;

            extension = path.ExtensionWithDot;
            fileName = path.NameWithoutExtension;
            filePath =  String.Join("/", pathComponents);

            return true;
        }

        private class UnityChildPathSceneConsumer : IUnitySceneProcessorConsumer
        {
            public readonly List<int> RootIndices = new List<int>();
            
            public void ConsumeGameObject(IYamlDocument gameObject, IBlockMappingNode modifications)
            {
                {
                    int rootOrder = -1;
                    var transform = UnityObjectPsiUtil.FindTransformComponentForGameObject(gameObject);
                    if (modifications != null)
                    {
                        if (!int.TryParse(UnityObjectPsiUtil.GetValueFromModifications(modifications, transform.GetFileId(), UnityYamlConstants.RootOrderProperty)
                            , out rootOrder))
                            rootOrder = -1;
                    }
                    if (rootOrder == -1)
                    {
                        var rootOrderAsString = transform.GetUnityObjectPropertyValue(UnityYamlConstants.RootOrderProperty).AsString();
                        if (!int.TryParse(rootOrderAsString, out rootOrder))
                            rootOrder = -1;
                    }
                    RootIndices.Add(rootOrder);
                }
            }
        }
    }
}