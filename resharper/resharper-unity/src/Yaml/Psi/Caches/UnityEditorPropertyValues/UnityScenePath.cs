using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Swa
{
    [SolutionComponent]
    public class UnityScenePath
    {
        private readonly SolutionAnalysisConfiguration mySolutionAnalysisConfiguration;
        private readonly UnitySceneProcessor mySceneProcessor;
        private readonly UnityPropertyValueCache myUnityPropertyValueCache;
        private readonly UnityGameObjectNamesCache myUnityGameObjectNamesCache;
        private readonly MetaFileGuidCache myCache;

        public UnityScenePath(SolutionAnalysisConfiguration solutionAnalysisConfiguration, UnitySceneProcessor sceneProcessor,
            UnityPropertyValueCache unityPropertyValueCache, UnityGameObjectNamesCache unityGameObjectNamesCache, MetaFileGuidCache cache)
        {
            mySolutionAnalysisConfiguration = solutionAnalysisConfiguration;
            mySceneProcessor = sceneProcessor;
            myUnityPropertyValueCache = unityPropertyValueCache;
            myUnityGameObjectNamesCache = unityGameObjectNamesCache;
            myCache = cache;
        }

        public List<string> GetScenePathSafe(IYamlDocument yamlDocument, bool fastName, string defaultName = "UNKNOWN")
        {
            return GetScenePath(yamlDocument, fastName) ?? new List<string> {defaultName};
        }
        
        public List<string> GetScenePath(IYamlDocument yamlDocument, bool fastName)
        {
            var anchor = yamlDocument.GetAnchor();
            var sourceFile = yamlDocument.GetSourceFile();
            if (sourceFile == null)
                return null;
            var guid =  myCache.GetAssetGuid(sourceFile);
            if (guid == null)
                return null;
            
            var consumer = new UnityPathCachedSceneConsumer();
            myUnityPropertyValueCache.UnitySceneDataLocalCache.ProcessSceneHierarchyFromComponentToRoot(yamlDocument, consumer);
            return consumer.NameParts;
        }
    }
}