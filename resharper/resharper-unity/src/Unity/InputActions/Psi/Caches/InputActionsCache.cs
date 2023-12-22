using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;
using JetBrains.Util.Collections;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches
{
    [PsiComponent]
    public class InputActionsCache : SimpleICache<List<InputActionsCacheItem>>
    {
        private readonly IShellLocks myShellLocks;
        private readonly OneToListMap<IPsiSourceFile, InputActionsCacheItem> myLocalCache = new();
        private readonly CountingSet<string> myMethodNames = new();
        private readonly OneToListMap<IPsiSourceFile, InputActionsDeclaredElement> myDeclaredElements = new();

        public InputActionsCache(Lifetime lifetime,
            IShellLocks shellLocks,
            IPersistentIndexManager persistentIndexManager)
            : base(lifetime, shellLocks, persistentIndexManager, InputActionsCacheItem.Marshaller)
        {
            myShellLocks = shellLocks;
        }
        
        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return base.IsApplicable(sf) && sf.IsInputActions() && sf.IsLanguageSupported<JsonNewLanguage>();
        }

        public override object? Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            var file = sourceFile.GetDominantPsiFile<JsonNewLanguage>() as IJsonNewFile;
            var rootObject = file?.GetRootObject();

            var results = new List<InputActionsCacheItem>();

            if (rootObject == null) return results;
            var maps = rootObject.MembersEnumerable.FirstOrDefault(member => member.Key == "maps")?.Value; // maps can be only once
            if (maps is not IJsonNewArray mapsArray) return results;
            var members = mapsArray.Values.SelectMany(a => ((IJsonNewObject)a).MembersEnumerable);
            var actions = members.Where(m => m is { Key: "actions", Value: IJsonNewArray }) // actions may appear multiple times
                .SelectNotNull(a=>a.Value).OfType<IJsonNewArray>().SelectMany(a=>a.Values);
            var possibleNames = actions.SelectMany(a => ((IJsonNewObject)a).MembersEnumerable);
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
            myDeclaredElements.RemoveKey(sourceFile);
        }

        private void AddToLocalCache(IPsiSourceFile sourceFile, List<InputActionsCacheItem>? items)
        {
            if (items == null) return;
            myLocalCache.AddValueRange(sourceFile, items);
            foreach (var item in items)
            {
                myMethodNames.Add(item.Name);
            }
            myDeclaredElements.AddValueRange(sourceFile, items.Select(a=>CreateDeclaredElements(sourceFile, a)));
        }
        
        private InputActionsDeclaredElement CreateDeclaredElements(IPsiSourceFile sourceFile, InputActionsCacheItem item)
        {
            return new InputActionsDeclaredElement(item.Name, sourceFile, item.DeclarationOffset);
        }

        public IEnumerable<InputActionsDeclaredElement> GetDeclaredElements(VirtualFileSystemPath file, string name)
        {
            myShellLocks.AssertReadAccessAllowed();
            var list = myDeclaredElements.Where(a => a.Key.GetLocation() == file).SelectMany(a=>a.Value); // https://youtrack.jetbrains.com/issue/RIDER-85555/Include-inputactions-file-into-csproj-and-generate-.Player-projects
            return list.Where(element => element.ShortName == name);
        }

        public bool ContainsName(string name)
        {
            myShellLocks.AssertReadAccessAllowed();
            return myMethodNames.Contains(name);
        }
    }
}