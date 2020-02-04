using System;
using System.Collections.Concurrent;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy
{
    [SolutionComponent]
    public class AssetDocumentHierarchyElementContainer : IUnityAssetDataElementContainer
    {
        private static readonly StringSearcher ourMonoBehaviourCheck = new StringSearcher("!u!114", true);

        private static bool IsMonoBehaviourDocument(AssetDocument document) =>
            ourMonoBehaviourCheck.Find(document.Buffer, 0, Math.Min(document.Buffer.Length, 20)) > 0;
        
        
        private readonly ConcurrentDictionary<IPsiSourceFile, AssetDocumentHierarchyElement> myAssetDocumentsHierarchy =
            new ConcurrentDictionary<IPsiSourceFile, AssetDocumentHierarchyElement>();

        public IUnityAssetDataElement Build(Lifetime lifetime, IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
        {
            if (IsMonoBehaviourDocument(assetDocument))
            {
                var entries = assetDocument.Document.FindRootBlockMapEntries()?.Entries;
                if (entries == null)
                    return null;

                var anchor = assetDocument.Document.GetLocalDocumentAnchor();
                AssetDocumentReference documentReference = null;

                foreach (var entry in entries)
                {
                    if (entry.Key.MatchesPlainScalarText(UnityYamlConstants.ScriptProperty))
                    {
                        documentReference = entry.Content.Value.AsFileID();
                        break;
                    }
                }

                if (documentReference != null && anchor != null)
                {
                    return new AssetDocumentHierarchyElement(
                            new ScriptComponentHierarchy(new LocalReference(currentSourceFile.GetPersistentID(), anchor),
                            new ExternalReference(documentReference.ExternalAssetGuid, documentReference.LocalDocumentAnchor)));
                }
            }
            return null;
        }

        public void Drop(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
        {
            myAssetDocumentsHierarchy.TryRemove(sourceFile, out _);
        }

        public void Merge(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
        {
            myAssetDocumentsHierarchy[sourceFile] = unityAssetDataElement as AssetDocumentHierarchyElement;
        }

        public string Id => nameof(AssetDocumentHierarchyElementContainer);
    }
}