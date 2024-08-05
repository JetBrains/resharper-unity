using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Dependencies;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    [SolutionComponent(InstantiationEx.LegacyDefault)]
    public class MetaFileImageContributor : IFileImageContributor
    {
        private readonly VirtualFileSystemPath mySolutionDirectory;

        public MetaFileImageContributor(ISolution solution)
        {
            mySolutionDirectory = ResourceLoadCache.GetSolutionDirectoryPath(solution);
        }

        public IEnumerable<KeyValuePair<Dependency, Hash>> SolutionDependencies(ISolution solution)
        {
            return EmptyDictionary<Dependency, Hash>.Instance;
        }

        public IEnumerable<KeyValuePair<Dependency, Hash>> ModuleDependencies(IPsiModule module)
        {
            return EmptyDictionary<Dependency, Hash>.Instance;
        }

        public IEnumerable<KeyValuePair<Dependency, Hash>> FileDependencies(IPsiSourceFile psiSourceFile)
        {
            var virtualFileSystemPath = psiSourceFile.GetLocation(); // /some/path/Assets/Resource/MyPrefab.prefab.meta
            if (!virtualFileSystemPath.IsMeta() && !virtualFileSystemPath.IsFromResourceFolder()) 
                yield break;
            virtualFileSystemPath = virtualFileSystemPath.ChangeExtension("");// /some/path/Assets/Resource/MyPrefab.prefab
            
            var relativeSourceFilePath = virtualFileSystemPath.TryMakeRelativeTo(mySolutionDirectory);
            
            var pathInsideResourcesFolder = ResourceLoadCache.GetPathInsideResourcesFolder(relativeSourceFilePath);
            if(pathInsideResourcesFolder.IsEmpty)
                yield break;

            yield return new KeyValuePair<Dependency, Hash>(
                ResourceLoadCache.CreateDependency(psiSourceFile, pathInsideResourcesFolder.NameWithoutExtension), 
                new Hash(17));
        }
    }
}