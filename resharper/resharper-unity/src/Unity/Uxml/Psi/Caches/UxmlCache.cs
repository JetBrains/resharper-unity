using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;
using JetBrains.Util.Collections;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Uxml.Psi.Caches
{
    [PsiComponent]
    public class UxmlCache : SimpleICache<List<UxmlCacheItem>>
    {
        private readonly PackageManager myPackageManager;
        private readonly OneToListMap<IPsiSourceFile, UxmlCacheItem> myLocalCache = new();
        private readonly CountingSet<string> myMethodNames = new();
        private readonly VirtualFileSystemPath myAssetsFolder;

        public UxmlCache(Lifetime lifetime,
            IShellLocks shellLocks,
            IPersistentIndexManager persistentIndexManager,
            ISolution solution,
            PackageManager packageManager)
            : base(lifetime, shellLocks, persistentIndexManager, UxmlCacheItem.Marshaller)
        {
            myPackageManager = packageManager;
            myAssetsFolder = solution.SolutionDirectory.Combine("Assets");
        }
        
        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return base.IsApplicable(sf) && sf.IsUxml() && sf.IsLanguageSupported<XmlLanguage>() 
                   && (myAssetsFolder.IsPrefixOf(sf.GetLocation()) || myPackageManager.IsLocalPackageCacheFile(sf.GetLocation()));
        }

        public override object? Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            if (sourceFile.GetDominantPsiFile<XmlLanguage>() is not IXmlFile file) return null;
            var results = new List<UxmlCacheItem>();

            var namespaces = ParseXmlNs();
            
            ProcessTagsRecursive(file, results, namespaces);
            
            return results;

            Dictionary<string, string> ParseXmlNs()
            {
                // uxml in the collab-proxy package has empty XmlName
                var namespaceList = new Dictionary<string, string>();
                foreach (var child in file.Children())
                {
                    if (child is not IXmlTag xmlTag) continue;
                    foreach (var attribute in xmlTag.Header.Attributes)
                    {
                        if (attribute.XmlNamespace != "xmlns") continue;
                        namespaceList.Add(attribute.XmlName, attribute.UnquotedValue);
                    }
                }

                return namespaceList;
            }
        }

        private void ProcessTagsRecursive(ITreeNode treeNode, List<UxmlCacheItem> results, Dictionary<string, string> namespaces)
        {
            if (treeNode is IXmlTag or IXmlTagHeader or IXmlFile)
            {
                foreach (var child in treeNode.Children())
                {
                    ProcessTagsRecursive(child, results, namespaces);    
                }
            }
            else if (treeNode is IXmlAttribute { AttributeName: "name" } xmlAttribute)
            {
                var xmlIdentifier = xmlAttribute.Parent?.Children<XmlIdentifier>().FirstOrDefault();
                if (xmlIdentifier != null && namespaces.TryGetValue(xmlIdentifier.XmlNamespace, out var ns))
                {
                    var item = new UxmlCacheItem($"{ns}.{xmlIdentifier.XmlName}", xmlAttribute.Value!.UnquotedValue,
                        xmlAttribute.Value.GetTreeStartOffset().Offset);
                    results.Add(item);    
                }
                else if (xmlIdentifier?.XmlNamespace == string.Empty)
                {
                    var item = new UxmlCacheItem(xmlIdentifier.XmlName, xmlAttribute.Value!.UnquotedValue,
                        xmlAttribute.Value.GetTreeStartOffset().Offset);
                    results.Add(item);
                }
            }
        }

        public override void Merge(IPsiSourceFile sourceFile, object? builtPart)
        {
            RemoveFromLocalCache(sourceFile);
            AddToLocalCache(sourceFile, builtPart as List<UxmlCacheItem>);
            base.Merge(sourceFile, builtPart);
        }

        public override void MergeLoaded(object data)
        {
            base.MergeLoaded(data);
            PopulateLocalCache();
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            RemoveFromLocalCache(sourceFile);
            base.Drop(sourceFile);
        }
        
        private void PopulateLocalCache()
        {
            foreach (var (file, cacheItems) in Map)
                AddToLocalCache(file, cacheItems);
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            var items = myLocalCache[sourceFile];
            foreach (var item in items)
            {
                myMethodNames.Remove(item.Name);
            }
            
            myLocalCache.RemoveKey(sourceFile);
        }

        private void AddToLocalCache(IPsiSourceFile sourceFile, List<UxmlCacheItem>? items)
        {
            if (items == null) return;
            myLocalCache.AddValueRange(sourceFile, items);
            foreach (var item in items)
            {
                myMethodNames.Add(item.Name);
            }
        }

        public IEnumerable<string> GetPossibleNames(string controlTypeName)
        {
            return myLocalCache.Values.Where(a => a.ControlTypeName == controlTypeName).Select(b=>b.Name).Distinct();
        }
        
        public IEnumerable<string> GetAllPossibleNames()
        {
            return myMethodNames.GetItems().Distinct();
        }
    }
}