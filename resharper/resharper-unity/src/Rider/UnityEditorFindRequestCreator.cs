using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Rider.Model;
using JetBrains.Util;
using JetBrains.Util.dataStructures;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityEditorFindRequestCreator
    {
        private readonly ISolution mySolution;
        private readonly UnityHost myUnityHost;
        private readonly FileSystemPath mySolutionDirectoryPath;

        public UnityEditorFindRequestCreator(ISolution solution, UnityHost unityHost)
        {
            mySolution = solution;
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

            var result = new List<FindUsageResult>();

            foreach (var reference in references)
            {
                var request = CreateRequest(reference, selectedReference);
                if (request != null)
                    result.Add(request);
            }

            if (result.Count == 0)
                return false;

            if (focusUnity)
            {
                UnityFocusUtil.FocusUnity(myUnityHost.GetValue(t => t.UnityProcessId.Value));
            }
            
            if (selectedReference != null)
                myUnityHost.PerformModelAction(t => t.ShowGameObjectOnScene.Fire(CreateRequest(selectedReference, null)));
            myUnityHost.PerformModelAction(t => t.FindUsageResults.Fire(result.ToArray()));
            return true;
        }
        
        private FindUsageResult CreateRequest([NotNull] IUnityYamlReference currentReference, [CanBeNull] IUnityYamlReference selectedReference)
        {
            var gameObjectDocument = currentReference.ComponentDocument.GetUnityObjectDocumentFromFileIDProperty(UnityYamlConstants.GameObjectProperty) ?? currentReference.ComponentDocument;

            var sourceFile = gameObjectDocument?.GetSourceFile();
            if (sourceFile == null)
                return null;

            var pathElements = UnityObjectPsiUtil.GetGameObjectPathFromComponent(currentReference.ComponentDocument).RemoveEnd("\\").Split("\\");
            var rootIndices = new FrugalLocalList<int>();
            
            // Constructing path of child indices
            UnityObjectPsiUtil.ProcessToRoot(gameObjectDocument,(currentGameObject, modification) =>
                {
                    int rootOrder = -1;
                    var transform = UnityObjectPsiUtil.FindTransformComponentForGameObject(currentGameObject);
                    if (modification != null)
                    {
                        if (!int.TryParse(UnityObjectPsiUtil.GetValueFromModifications(modification, transform.GetFileId(), UnityYamlConstants.RootOrderProperty)
                            , out rootOrder))
                            rootOrder = -1;
                    }
                    if (rootOrder == -1)
                    {
                        var rootOrderAsString = transform.GetUnityObjectPropertyValue(UnityYamlConstants.RootOrderProperty).AsString();
                        if (!int.TryParse(rootOrderAsString, out rootOrder))
                            rootOrder = -1;
                    }
                    rootIndices.Add(rootOrder);
                });


            if (!GetPathFromAssetFolder(sourceFile, out var pathFromAsset, out var fileName, out var extension))
                return null;
            bool needExpand = currentReference == selectedReference;
            bool isPrefab = extension.Equals(UnityYamlConstants.Prefab, StringComparison.OrdinalIgnoreCase);
            
            return new FindUsageResult(isPrefab, needExpand, pathFromAsset, fileName, pathElements, rootIndices.ToArray().Reverse().ToArray());
        }

        private bool GetPathFromAssetFolder([NotNull] IPsiSourceFile file, out string filePath, out string fileName, out string extension)
        {
            extension = null;
            filePath = null;
            fileName = null;
            var path = file.GetLocation().MakeRelativeTo(mySolutionDirectoryPath);
            var assetFolder = path.FirstComponent;
            if (!assetFolder.Equals(UnityYamlConstants.AssetsFolder)) 
                return false;
            
            var pathComponents = path.GetPathComponents();

            extension = path.ExtensionWithDot;
            fileName = path.NameWithoutExtension;
            filePath =  String.Join("/", pathComponents);

            return true;
        }
    }
}