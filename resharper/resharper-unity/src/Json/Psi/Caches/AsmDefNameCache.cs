using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Json.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Impl.Resolve;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;
using JetBrains.ReSharper.Psi.JavaScript.Services;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Psi.Caches
{
    [PsiComponent]
    public class AsmDefNameCache : SimpleICache<AsmDefCacheItem>
    {
        private readonly ISolution mySolution;

        private readonly Dictionary<IPsiSourceFile, AsmDefNameDeclaredElement> myDeclaredElements =
            new Dictionary<IPsiSourceFile, AsmDefNameDeclaredElement>();

        public AsmDefNameCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager, ISolution solution)
            : base(lifetime, persistentIndexManager, AsmDefCacheItem.Marshaller)
        {
            mySolution = solution;
#if DEBUG
            ClearOnLoad = true;
#endif
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
            var elements = Map.Select(i =>
            {
                if (!myDeclaredElements.ContainsKey(i.Key))
                {
                    lock (this)
                    {
                        if (!myDeclaredElements.ContainsKey(i.Key))
                        {
                            var element = new AsmDefNameDeclaredElement(mySolution.GetComponent<JavaScriptServices>(),
                                i.Value.Name, i.Key, i.Value.DeclarationOffset);
                            myDeclaredElements.Add(i.Key, element);
                            return element;
                        }
                    }
                }
                return myDeclaredElements[i.Key];
            }).ToList();

            if (elements.IsEmpty())
                return EmptySymbolTable.INSTANCE;
            var psiServices = elements.First().GetPsiServices();
            return new DeclaredElementsSymbolTable<IDeclaredElement>(psiServices, elements);
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            var file = sourceFile.GetDominantPsiFile<JsonLanguage>();
            if (file == null)
                return null;

            var cacheBuilder = new AsmDefCacheItemBuilder();
            var processor = new RecursiveElementProcessor<IJavaScriptLiteralExpression>(e =>
            {
                // Only accept the first name. If there are multiple name definitions, it's an error, and that's just tough luck
                if (e.IsNameStringLiteralValue() && !cacheBuilder.HasNameDefinition)
                    cacheBuilder.SetNameDefinition(e.GetStringValue(), e.GetTreeStartOffset().Offset);
                else if (e.IsReferencesStringLiteralValue())
                    cacheBuilder.AddReference(e.GetStringValue());
            });
            file.ProcessDescendants(processor);

            return cacheBuilder.Build();
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            CleanLocalCache(sourceFile);
            base.Merge(sourceFile, builtPart);
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            CleanLocalCache(sourceFile);
            base.Drop(sourceFile);
        }

        private void CleanLocalCache(IPsiSourceFile sourceFile)
        {
            myDeclaredElements.Remove(sourceFile);
        }

        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return base.IsApplicable(sf) && sf.IsAsmDef() && sf.IsLanguageSupported<JsonLanguage>();
        }
    }
}