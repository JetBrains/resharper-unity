using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.AsmDefCommon.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Impl.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Psi.Caches
{
    [PsiComponent]
    public class AsmDefNameCache : SimpleICache<AsmDefCacheItem>
    {
        private readonly IShellLocks myShellLocks;
        private readonly ISolution mySolution;

        private readonly Dictionary<IPsiSourceFile, AsmDefNameDeclaredElement> myDeclaredElements =
            new Dictionary<IPsiSourceFile, AsmDefNameDeclaredElement>();

        private readonly OneToListMap<string, IPsiSourceFile> myNames = new OneToListMap<string, IPsiSourceFile>();
        
        public AsmDefNameCache(Lifetime lifetime, IShellLocks shellLocks, IPersistentIndexManager persistentIndexManager, ISolution solution)
            : base(lifetime, shellLocks, persistentIndexManager, AsmDefCacheItem.Marshaller)
        {
            myShellLocks = shellLocks;
            mySolution = solution;
        }

        [CanBeNull]
        public AsmDefNameDeclaredElement GetNameDeclaredElement(IPsiSourceFile sourceFile)
        {
            if (myDeclaredElements.TryGetValue(sourceFile, out var declaredElement))
                return declaredElement;
            return null;
        }

        // Returns a symbol table for all items. Used to resolve references and provide completion
        public ISymbolTable GetSymbolTable()
        {
            if (myDeclaredElements.IsEmpty())
                return EmptySymbolTable.INSTANCE;
            var psiServices = mySolution.GetComponent<IPsiServices>();
            return new DeclaredElementsSymbolTable<IDeclaredElement>(psiServices, myDeclaredElements.Values);
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            var file = sourceFile.GetDominantPsiFile<JsonNewLanguage>();
            if (file == null)
                return null;

            var cacheBuilder = new AsmDefCacheItemBuilder();
            var processor = new RecursiveElementProcessor<IJsonNewLiteralExpression>(e =>
            {
                // Only accept the first name. If there are multiple name definitions, it's an error, and that's just tough luck
                if (e.IsNameLiteral() && !cacheBuilder.HasNameDefinition)
                    cacheBuilder.SetNameDefinition(e.GetStringValue(), e.GetTreeStartOffset().Offset);
                else if (e.IsReferenceLiteral())
                    cacheBuilder.AddReference(e.GetStringValue());
            });
            file.ProcessDescendants(processor);

            return cacheBuilder.Build();
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            RemoveFromLocalCache(sourceFile);
            AddToLocalCache(sourceFile, builtPart as AsmDefCacheItem);
            base.Merge(sourceFile, builtPart);
        }

        public override void MergeLoaded(object data)
        {
            PopulateLocalCache();
            base.MergeLoaded(data);
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            RemoveFromLocalCache(sourceFile);
            base.Drop(sourceFile);
        }

        private void PopulateLocalCache()
        {
            foreach (var (sourceFile, cacheItem) in Map)
                AddToLocalCache(sourceFile, cacheItem);
        }

        private void AddToLocalCache(IPsiSourceFile sourceFile, [CanBeNull] AsmDefCacheItem asmDefCacheItem)
        {
            if (asmDefCacheItem == null) return;

            myNames.Add(asmDefCacheItem.Name, sourceFile);
            if (!myDeclaredElements.ContainsKey(sourceFile))
                myDeclaredElements.Add(sourceFile, CreateDeclaredElement(sourceFile, asmDefCacheItem));
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            var item = Map.GetValueSafe(sourceFile);
            if (item != null)
                myNames.Remove(item.Name, sourceFile);
            
            
            myDeclaredElements.Remove(sourceFile);
        }

        private AsmDefNameDeclaredElement CreateDeclaredElement(IPsiSourceFile sourceFile, AsmDefCacheItem cacheItem)
        {
            return new AsmDefNameDeclaredElement(cacheItem.Name, sourceFile, cacheItem.DeclarationOffset);
        }

        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return base.IsApplicable(sf) && sf.IsAsmDef() && sf.IsLanguageSupported<JsonNewLanguage>();
        }

        public FileSystemPath GetPathFor(string name)
        {
            myShellLocks.AssertReadAccessAllowed();
            return myNames.GetValuesSafe(name).FirstOrDefault(null)?.GetLocation();
        }
    }
}