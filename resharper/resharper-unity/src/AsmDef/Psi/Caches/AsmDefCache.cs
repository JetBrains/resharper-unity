using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Impl.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Threading;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches
{
    [PsiComponent]
    public class AsmDefCache : SimpleICache<AsmDefCacheItem>
    {
        private readonly ILogger myLogger;
        private readonly ISolution mySolution;

        private readonly Dictionary<IPsiSourceFile, AsmDefNameDeclaredElement> myDeclaredElements = new();
        private readonly OneToListMap<string, IPsiSourceFile> myNames = new();
        private readonly GroupingEvent myCacheUpdatedGroupingEvent;

        public AsmDefCache(Lifetime lifetime,
                           ISolution solution,
                           IShellLocks shellLocks,
                           IPersistentIndexManager persistentIndexManager,
                           ILogger logger)
            : base(lifetime, shellLocks, persistentIndexManager, AsmDefCacheItem.Marshaller)
        {
            myLogger = logger;
            mySolution = solution;

            myCacheUpdatedGroupingEvent = Locks.CreateGroupingEvent(lifetime, "Unity::AsmDefCacheUpdated",
                TimeSpan.FromMilliseconds(500));
        }

        public override string Version => "4";

        public ISimpleSignal CacheUpdated => myCacheUpdatedGroupingEvent.Outgoing;

        public bool IsKnownAssemblyDefinition(string assemblyName) => GetSourceFileForAssembly(assemblyName) != null;

        public AsmDefNameDeclaredElement? GetNameDeclaredElement(IPsiSourceFile sourceFile)
        {
            return myDeclaredElements.TryGetValue(sourceFile, out var declaredElement) ? declaredElement : null;
        }

        public AsmDefNameDeclaredElement? GetNameDeclaredElement(VirtualFileSystemPath sourceFilePath)
        {
            foreach (var (file, element) in myDeclaredElements)
            {
                if (file.GetLocation() == sourceFilePath)
                    return element;
            }

            return null;
        }

        // Returns a symbol table for all items. Used to resolve references and provide completion
        public ISymbolTable GetAssemblyNamesSymbolTable()
        {
            if (myDeclaredElements.IsEmpty())
                return EmptySymbolTable.INSTANCE;
            var psiServices = mySolution.GetComponent<IPsiServices>();
            return new DeclaredElementsSymbolTable<IDeclaredElement>(psiServices, myDeclaredElements.Values);
        }

        // Note that this is getting the location of a .asmdef file for a given assembly definition name. The name of
        // the file might not match the name of the assembly definition!
        public VirtualFileSystemPath? GetAsmDefLocationByAssemblyName(string assemblyName)
        {
            Locks.AssertReadAccessAllowed();
            return GetSourceFileForAssembly(assemblyName)?.GetLocation();
        }

        public IEnumerable<AsmDefVersionDefine> GetVersionDefines(string assemblyName)
        {
            var sourceFile = GetSourceFileForAssembly(assemblyName);
            if (sourceFile == null)
                return EmptyList<AsmDefVersionDefine>.Enumerable;

            return Map!.GetValueSafe(sourceFile)?.VersionDefines ?? EmptyList<AsmDefVersionDefine>.Enumerable;
        }

        public override object? Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            var file = sourceFile.GetDominantPsiFile<JsonNewLanguage>() as IJsonNewFile;
            var rootObject = file?.GetRootObject();

            var nameProperty = rootObject?.GetFirstPropertyValue<IJsonNewLiteralExpression>("name");
            var name = nameProperty?.GetStringValue();
            if (name == null || nameProperty == null)
                return null;

            var cacheBuilder = new AsmDefCacheItemBuilder(name, nameProperty.GetTreeStartOffset().Offset);

            var referencesProperty = rootObject.GetFirstPropertyValue<IJsonNewArray>("references");
            foreach (var entry in referencesProperty.ValuesAsLiteral())
                cacheBuilder.AddReference(entry.GetStringValue());

            var versionDefinesProperty = rootObject.GetFirstPropertyValue<IJsonNewArray>("versionDefines");
            foreach (var versionDefine in versionDefinesProperty.ValuesAsObject())
            {
                var resourceName = versionDefine.GetFirstPropertyValueText("name");
                var expression = versionDefine.GetFirstPropertyValueText("expression");
                var symbol = versionDefine.GetFirstPropertyValueText("define");

                // Note that expression can be empty! This means we only care if the pacakge/resource is available
                if (expression == null || string.IsNullOrWhiteSpace(resourceName) || string.IsNullOrWhiteSpace(symbol))
                    continue;

                // string.IsNullOrWhitespace isn't annotated...
                if (resourceName == null || symbol == null)
                    continue;

                cacheBuilder.AddVersionDefine(resourceName, symbol, expression);
            }

            return cacheBuilder.Build();
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            RemoveFromLocalCache(sourceFile);
            AddToLocalCache(sourceFile, builtPart as AsmDefCacheItem);
            base.Merge(sourceFile, builtPart);
            myCacheUpdatedGroupingEvent.FireIncoming();
        }

        public override void MergeLoaded(object data)
        {
            PopulateLocalCache();
            base.MergeLoaded(data);
            myCacheUpdatedGroupingEvent.FireIncoming();
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            RemoveFromLocalCache(sourceFile);
            base.Drop(sourceFile);
            myCacheUpdatedGroupingEvent.FireIncoming();
        }

        private void PopulateLocalCache()
        {
            foreach (var (sourceFile, cacheItem) in Map)
                AddToLocalCache(sourceFile, cacheItem);
        }

        private void AddToLocalCache(IPsiSourceFile sourceFile, AsmDefCacheItem? asmDefCacheItem)
        {
            if (asmDefCacheItem == null) return;

            myNames.Add(asmDefCacheItem.Name, sourceFile);
            if (!myDeclaredElements.ContainsKey(sourceFile))
                myDeclaredElements.Add(sourceFile, CreateDeclaredElement(sourceFile, asmDefCacheItem));
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            var item = Map!.GetValueSafe(sourceFile);
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

        private IPsiSourceFile? GetSourceFileForAssembly(string assemblyName)
        {
            var sourceFiles = myNames.GetValuesSafe(assemblyName);
            if (sourceFiles.Count > 1 && myLogger.IsWarnEnabled())
            {
                var files = string.Join(", ", sourceFiles);
                myLogger.Warn($"Multiple asmdef files found for assembly {assemblyName}: {files}");
            }

            return sourceFiles.FirstOrDefault();
        }
    }
}