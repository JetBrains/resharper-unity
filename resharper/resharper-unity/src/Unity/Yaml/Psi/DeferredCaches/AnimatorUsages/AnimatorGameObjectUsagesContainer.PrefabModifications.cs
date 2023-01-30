using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetScriptUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages
{
    public partial class AnimatorGameObjectUsagesContainer
    {
        private static LocalList<AssetScriptUsage> ProcessPrefabModifications(IPsiSourceFile currentFile, AssetDocument document)
        {
            var result = new LocalList<AssetScriptUsage>();
            if (document.HierarchyElement is not IPrefabInstanceHierarchy prefabInstanceHierarchy) return result;
            foreach (var modification in prefabInstanceHierarchy.PrefabModifications)
            {
                if (modification.Target is not ExternalReference target)
                    continue;

                if (modification.PropertyPath != UnityYamlConstants.ControllerProperty)
                    continue;
                    
                var location = new LocalReference(currentFile.PsiStorage.PersistentIndex.NotNull("owningPsiPersistentIndex != null"), 
                    PrefabsUtil.GetImportedDocumentAnchor(prefabInstanceHierarchy.Location.LocalDocumentAnchor, target.LocalDocumentAnchor));

                if (modification.ObjectReference is ExternalReference externalReference)
                {
                    result.Add(new AssetScriptUsage(location, externalReference));
                }
            }

            return result;
        }
    }
}