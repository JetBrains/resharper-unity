using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree.Impl;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Search
{
    [PsiSharedComponent]
    public class AsmDefGuidUsageSearchFactory : DomainSpecificSearcherFactoryBase
    {
        public override bool IsCompatibleWithLanguage(PsiLanguageType languageType) =>
            languageType.Is<JsonNewLanguage>();

        public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
        {
            if (element is not AsmDefNameDeclaredElement)
                yield break;

            var solution = element.GetSolution();
            var asmDefNameCache = solution.GetComponent<AsmDefNameCache>();
            var metaFileGuidCache = solution.GetComponent<MetaFileGuidCache>();

            var guid = GetGuid(element, asmDefNameCache, metaFileGuidCache);
            if (guid != null)
                yield return guid;
        }

        public override IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements, bool findCandidates)
        {
            if (elements.Any(e => e is not AsmDefNameDeclaredElement))
                return null;

            var solution = elements.First().GetSolution();
            var asmDefNameCache = solution.GetComponent<AsmDefNameCache>();
            var metaFileGuidCache = solution.GetComponent<MetaFileGuidCache>();

            var guids = new List<string>();
            foreach (var element in elements)
            {
                var guid = GetGuid(element, asmDefNameCache, metaFileGuidCache);
                if (guid != null)
                    guids.Add(guid);
            }

            return new AsmDefReferenceSearcher(elements, guids, findCandidates);
        }

        [CanBeNull]
        private static string GetGuid(IDeclaredElement element, AsmDefNameCache asmDefNameCache,
                                      MetaFileGuidCache metaFileGuidCache)
        {
            var asmDefLocation = asmDefNameCache.GetPathFor(element.ShortName);
            if (asmDefLocation != null)
            {
                var assetGuid = metaFileGuidCache.GetAssetGuid(asmDefLocation);
                if (assetGuid.HasValue)
                    return assetGuid.Value.ToString("N");
            }

            return null;
        }
    }

    public class AsmDefReferenceSearcher : IDomainSpecificSearcher
    {
        private readonly IDeclaredElementsSet myElements;
        private readonly bool myFindCandidates;
        private readonly List<string> myElementNames;
        private readonly List<string> myElementGuids;

        public AsmDefReferenceSearcher(IDeclaredElementsSet elements, List<string> guids, bool findCandidates)
        {
            myElements = elements;
            myElementGuids = guids;
            myFindCandidates = findCandidates;

            myElementNames = new List<string>();
            foreach (var element in elements)
                myElementNames.Add(element.ShortName);
        }

        public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
        {
            if (!sourceFile.IsAsmDef() || sourceFile.GetPrimaryPsiFile() is not JsonNewFile jsonNewFile)
                return false;
            return ProcessElement(jsonNewFile, consumer);
        }

        public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
        {
            var result = new GuidReferenceSearchSourceFileProcessor<TResult>(element, myFindCandidates, consumer,
                myElements, myElementNames, myElementGuids).Run();
            return result == FindExecution.Stop;
        }

        // We can't use ReferenceSearchSourceFileProcessor as-is, because it will do a case sensitive search, because
        // JSON is case sensitive, and so is our name element, but our GUID references are not.
        // The wordsInText parameter is used to create a set of StringSearchers (case sensitive) to see if we should
        // visit a scope.
        // The referenceNames parameter is used to create a ReferenceNameContainer used to filter out references to
        // process.
        // If we pass null for both parameters, ReferenceSearchSourceFileProcessor will walk the entire PSI, and call
        // PreFilterReference for each reference. We can override PreFilterReference to only look at AsmDefNameReference
        // with a matching name, or with a GUID: name. This will get resolved and matched, and we're good. But we will
        // still walk the entire PSI
        // If we pass the proper name in wordsInText, we get a valid StringSearcher and RSSP will look for named
        // references, and will call SubTreeContainsText to skip a PSI subtree. We can override this to call base for
        // the proper name search, and provide our own case insensitive GUID searcher.
        // We can't pass anything into referenceNames, as it will be case sensitive, and there is no way to override
        // this filter.
        private class GuidReferenceSearchSourceFileProcessor<TResult> : ReferenceSearchSourceFileProcessor<TResult>
        {
            private readonly IEnumerable<StringSearcher> myStringSearchers;

            public GuidReferenceSearchSourceFileProcessor(ITreeNode treeNode, bool findCandidates,
                                                          IFindResultConsumer<TResult> resultConsumer,
                                                          IDeclaredElementsSet elements,
                                                          List<string> elementNames,
                                                          List<string> elementGuids)
                : base(treeNode, findCandidates, resultConsumer, elements, elementNames, null)
            {
                myStringSearchers = elementGuids.Select(g => new StringSearcher(g, false));
            }

            protected override bool PreFilterReference(IReference reference) => reference is AsmDefNameReference &&
                reference.GetName().StartsWith("guid:", StringComparison.InvariantCultureIgnoreCase);

            protected override bool SubTreeContainsText(ITreeNode node)
            {
                var buffer = node.GetTextAsBuffer();
                return BufferContainsText(buffer) || myStringSearchers.Any(s => s.Find(buffer) >= 0);
            }
        }
    }
}