using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Impl.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches
{
    [PsiComponent]
    public class InputActionsCache : SimpleICache<List<InputActionsCacheItem>>
    {
        private readonly IShellLocks myShellLocks;
        private readonly ISolution mySolution;
        // private readonly Dictionary<string, HashSet<Guid>> methodNameToGuids = new(); // inputactions
        private readonly OneToListMap<IPsiSourceFile, List<InputActionsCacheItem>> myLocalCache = new();
        private readonly Dictionary<IPsiSourceFile, List<InputActionsDeclaredElement>> myDeclaredElements = new();

        public InputActionsCache(Lifetime lifetime,
            IShellLocks shellLocks, ISolution solution,
            IPersistentIndexManager persistentIndexManager)
            : base(lifetime, shellLocks, persistentIndexManager, InputActionsCacheItem.Marshaller)
        {
            myShellLocks = shellLocks;
            mySolution = solution;
        }

        public override object? Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            var file = sourceFile.GetDominantPsiFile<JsonNewLanguage>() as IJsonNewFile;
            var rootObject = file?.GetRootObject();

            var results = new List<InputActionsCacheItem>();

            if (rootObject == null) return results;
            var maps = rootObject.MembersEnumerable.FirstOrDefault(member => member.Key == "maps")?.Value;
            if (maps is not IJsonNewArray mapsArray) return results;
            var members = mapsArray.Values.SelectMany(a => ((IJsonNewObject)a).MembersEnumerable);
            var actions = members.FirstOrDefault(m => m is { Key: "actions", Value: IJsonNewArray })?.Value;
            if (actions is not IJsonNewArray actionsArray) return results;
            var possibleNames =
                actionsArray.Values.SelectMany(a => ((IJsonNewObject)a).MembersEnumerable);
            var nameObjects = possibleNames.Where(nameMember => nameMember.Key == "name").SelectNotNull(a=>a.Value);
            foreach (var nameObject in nameObjects)
            {
                if (nameObject is not IJsonNewLiteralExpression nameProperty) continue;
                results.Add(new InputActionsCacheItem(nameProperty.GetStringValue(),
                    nameProperty.GetTreeStartOffset().Offset));
            }
            
            return results;
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            RemoveFromLocalCache(sourceFile);
            AddToLocalCache(sourceFile, builtPart as List<InputActionsCacheItem>);
            base.Merge(sourceFile, builtPart);
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            myLocalCache.RemoveKey(sourceFile);
            myDeclaredElements.Remove(sourceFile);
        }

        private void AddToLocalCache(IPsiSourceFile sourceFile, List<InputActionsCacheItem>? item)
        {
            if (item == null) return;
            myLocalCache.Add(sourceFile, item);
            if (!myDeclaredElements.ContainsKey(sourceFile))
                myDeclaredElements.Add(sourceFile, CreateDeclaredElements(sourceFile, item));
        }

        private List<InputActionsDeclaredElement> CreateDeclaredElements(IPsiSourceFile sourceFile, List<InputActionsCacheItem> item)
        {
            return item.Select(a=>new InputActionsDeclaredElement(a.Name, sourceFile, a.DeclarationOffset)).ToList();
        }

        public override void MergeLoaded(object data)
        {
            foreach (var inputActionCacheItem in Map)
                AddToLocalCache(inputActionCacheItem.Key, inputActionCacheItem.Value);
            base.MergeLoaded(data);
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            RemoveFromLocalCache(sourceFile);
            base.Drop(sourceFile);
        }

        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return base.IsApplicable(sf) && sf.IsInputActions() && sf.IsLanguageSupported<JsonNewLanguage>();
        }
        
        private List<InputActionsDeclaredElement> Get(IPsiSourceFile sf)
        {
            return myDeclaredElements[sf];
        }
        
        public bool ContainsOffset(IPsiSourceFile file, ITreeNode node)
        {
            return ContainsOffset(file, node.GetTreeStartOffset().Offset);
        }
        
        public  bool ContainsOffset(IPsiSourceFile file, int offset)
        {
            return myDeclaredElements[file]
                .Any(a => a.DeclarationOffset == offset);
        }
        
        public bool ContainsNameForFile(IPsiSourceFile file, string name)
        {
            var list = myDeclaredElements[file];
            return list.Any(element => element.ShortName == name);
        }
        
        // todo: improve. maybe cache Guid or VirtualFileSystemPath instead of IPsiSourceFile
        public bool ContainsNameForFile(VirtualFileSystemPath file, string name)
        {
            // ConcurrentDictionary<>
            // lock
            
            var list = myDeclaredElements.Single(a => a.Key.GetLocation() == file).Value;
            return list.Any(element => element.ShortName == name);
        }
        
        public IEnumerable<InputActionsDeclaredElement> GetDeclaredElements(VirtualFileSystemPath file, string name)
        {
            // ConcurrentDictionary<>
            // lock
            
            var list = myDeclaredElements.Single(a => a.Key.GetLocation() == file).Value;
            return list.Where(element => element.ShortName == name);
        }

        // Returns a symbol table for all items. Used to resolve references and provide completion
        public ISymbolTable GetSymbolTable()
        {
            if (myDeclaredElements.IsEmpty())
                return EmptySymbolTable.INSTANCE;
            var psiServices = mySolution.GetComponent<IPsiServices>();
            return new DeclaredElementsSymbolTable<IDeclaredElement>(psiServices, myDeclaredElements.SelectMany(a=>a.Value));
        }

        public IEnumerable<string> GetNames(IDeclaredElement element)
        {
            return myLocalCache.SelectMany(item => item.Value)
                .SelectMany(a => a).Select(a=>a.Name);
        }
    }
}