using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Text;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    // Allows extra filtering in FinderUtil.NarrowSearchDomain. This can have a positive impact on any find operation,
    // such as plain old Find Usages and also the find usages that happens before rename.
    // E.g. when performing a rename, ReSharper will use the finder to collect all element usages after a new name has
    // been entered. The search domain will include all files that contain, as text, the name of the item being renamed.
    // So renaming something called "GameObject" would include nearly all assets, scenes and prefabs, because
    // "GameObject" is used EVERYWHERE in Unity YAML files.
    // Our UnityYamlUsageSearchFactory.GetAllPossibleWordsInFile can add extra search terms, but this becomes an ANY
    // search rather than an ALL search. And UnityYamlUsageSearchFactory.CreateReferenceSearcher can return null if the
    // target declared element is not interesting, but finder will still process the file. Admittedly, this is fairly
    // lightweight, but we can do even better if we remove the files from the search domain early on.
    // Sadly, this doesn't affect scanning for suspicious references after a rename, which can still be very expensive
    // if we've renamed a commonly used term (such as "GameObject" or "x")
    //
    // NOTE: If someone adds a reference that this class doesn't know about, the file will be removed from the search
    // domain and any usages won't be found!
     // [SearchGuru(SearchGuruPerformanceEnum.FastFilterOutByIndex)]
    public class UnityYamlSearchGuru : ISearchGuru
    {
        private readonly UnityApi myUnityApi;
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly IEnumerable<ITrigramIndex> myIndices;

        public UnityYamlSearchGuru(UnityApi unityApi, MetaFileGuidCache metaFileGuidCache, IEnumerable<ITrigramIndex> indices)
        {
            myUnityApi = unityApi;
            myMetaFileGuidCache = metaFileGuidCache;
            myIndices = indices;
        }

        // Allows us to filter the words that are collected from IDomainSpecificSearchFactory.GetAllPossibleWordsInFile
        // This is an ANY search
        public IEnumerable<string> BuzzWordFilter(IDeclaredElement searchFor, IEnumerable<string> containsWords) =>
            containsWords;

        public bool IsAvailable(SearchPattern pattern) => (pattern & SearchPattern.FIND_USAGES) != 0;

        // Return a context object for the item being searched for, or null if the element isn't interesting.
        // CanContainReferences isn't called if we return null. Do the work once here, then use it multiple times for
        // each file in CanContainReferences
        public object GetElementId(IDeclaredElement element)
        {
            if (myUnityApi.IsUnityType(element as IClass))
            {
                var sourceFile = element.GetSourceFiles().FirstOrDefault();
                if (sourceFile == null)
                    return null;

                var guid = myMetaFileGuidCache.GetAssetGuid(sourceFile);
                if (guid == null)
                    return null;

                // Class usage is in the form: "m_Script: {fileID: 11500000, guid: $guid, ... }"
                // Get the set of files that contain ALL of these terms
                return GetElementId("m_Script", "11500000", guid);
            }

            // See RIDER-27684. This allows Unity to use private methods as event handlers
            if (myUnityApi.IsPotentialEventHandler(element as IMethod)
                || myUnityApi.IsPotentialEventHandler(element as IProperty))
            {
                // Get all files that contain GUID, m_MethodName, short name and 11500000
                var sourceFile = element.GetSourceFiles().FirstOrDefault();
                if (sourceFile == null)
                    return null;

                var shortName = element is IMethod ? element.ShortName : (element as IProperty)?.Setter?.ShortName;
                if (shortName == null)
                    return null;
                
                // In previous version we check that file should contain guid which relates to file
                // where method is located. It is not true, consider an example:
                // class A : B {} - guid_1
                // class B : MonoBehaviour - guid_2
                // { public void Test(){}}
                // Let's add button, which will use script A and method Test
                // In previous version we require that yaml file with button should contain guid_2,
                // but it contains only guid_1
                // [TODO] We could find derived classes' guids and add them to filter

                // Searching for an event handler method requires matching something like
                // Event handlers are in the form:
                // OnSomeEvent:
                //   m_PersistentCalls:
                //     m_Calls:
                //     - m_Target: {fileID: 878035745}
                //       m_MethodName: $shortName
                // and
                // --- !u!114 &878035745
                // MonoBehaviour:
                //   ...
                //   m_Script: {fileID: 11500000, guid: $guid, type: 3}
                
                // In some cases 1150000 is not presented...
                //   m_Script: {fileID: 1392445389, guid: f70555f144d8491a825f0804e09c671c, type: 3} Why?? Do not know
                return GetElementId("m_PersistentCalls", "m_MethodName", shortName);
            }

            return new UnityYamlSearchGuruId(JetHashSet<IPsiSourceFile>.Empty);
        }

        // False means definitely not, true means "maybe"
        public bool CanContainReferences(IPsiSourceFile sourceFile, object elementId)
        {
            // Meta files never contain references
            if (sourceFile.IsMeta())
                return false;

            if (sourceFile.IsAsset())
            {
                // We know the file matches ANY of the search terms, see if it also matches ALL of the search terms
                return ((UnityYamlSearchGuruId) elementId).Files.Contains(sourceFile);
            }

            // Not a YAML file, don't exclude it
            return true;
        }

        private UnityYamlSearchGuruId GetElementId([ItemNotNull] params string[] searchTerms)
        {
            var foundFiles = new JetHashSet<IPsiSourceFile>();
            foreach (var trigramIndex in myIndices)
            {
                // Search for files that contain ALL terms. We've already been filtered to files containing ANY term.
                // We can't check for word boundaries because the index requires a word to start with a letter, and we
                // can't guarantee that a guid will do so (and we search for "11500000", too)
                foundFiles.AddRange(trigramIndex.GetFilesContainingQueries(searchTerms));
            }
            return new UnityYamlSearchGuruId(foundFiles);
        }

        private class UnityYamlSearchGuruId
        {
            public JetHashSet<IPsiSourceFile> Files { get; }

            public UnityYamlSearchGuruId(JetHashSet<IPsiSourceFile> files)
            {
                Files = files;
            }
        }
    }
}