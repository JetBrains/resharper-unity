using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Refactorings.Rename;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    public class UnityEventTargetAtomicRename : AtomicRenameBase
    {
        private readonly ISolution mySolution;
        private readonly bool myIsRenameShouldBeSilent;
        private readonly IDeclaredElementPointer<IDeclaredElement> myPointer;
        private List<UnityEventHandlerOccurrence> myElementsToRename;
        private bool myIsProperty;
        public UnityEventTargetAtomicRename(ISolution solution, IDeclaredElement declaredElement, string newName,
            bool isRenameShouldBeSilent)
        {
            mySolution = solution;
            myIsRenameShouldBeSilent = isRenameShouldBeSilent;
            myPointer = declaredElement.CreateElementPointer();
            OldName = declaredElement.ShortName;
            NewName = newName;
        }

        public override IRefactoringPage CreateRenamesConfirmationPage(IRenameWorkflow renameWorkflow, IProgressIndicator pi)
        {
            // do not run find usages too, silent == true for player projects and misc project(misc could happen due to RIDER-53753)
            // If we run find usages for player/misc declared element, find usages will return empty result, because
            // guid is resolved to psiSourceFile from real projects only (GetTypeElementFromScriptAssetGuid in AssetUtils)
            
            // NOTE: find usages under the hood uses cache which stores TextRanges, cache will not be updated between several atomic renames.
            // That means that only one atomic rename should exist or only one atomic rename should return non-empty result from find usages below
            if (myIsRenameShouldBeSilent)
                return null;
            
            var de = myPointer.FindDeclaredElement();
            if (de == null)
                return null;
            myIsProperty = de is IProperty;
            
            pi.Start(1);
            using (var subProgress = pi.CreateSubProgress())
            {
                myElementsToRename = GetAssetOccurrence(de, subProgress)
                    .Select(t => 
                        new UnityEventHandlerOccurrence(t.SourceFile, de.CreateElementPointer(), t.OwningElemetLocation, t.AssetMethodUsages, t.IsPrefabModification)).ToList();
            }
            
            return new UnityEventTargetRefactoringPage(
                ((RefactoringWorkflowBase) renameWorkflow).WorkflowExecuterLifetime, mySolution.GetComponent<DeferredCacheController>());
        }

        private List<UnityEventHandlerFindResult> GetAssetOccurrence(IDeclaredElement de, IProgressIndicator subProgress)
        {
            var finder = mySolution.GetPsiServices().AsyncFinder;
            var module = mySolution.GetComponent<UnityExternalFilesModuleFactory>().PsiModule;
            var searchDomain = SearchDomainFactory.Instance.CreateSearchDomain(module);
            var results = new List<UnityEventHandlerFindResult>();

            var elements = de is IProperty property ? new[] {de, property.Getter, property.Setter} : new[] {de};
            
            finder.Find(elements.Where(t => t != null).ToArray(), searchDomain, new FindResultConsumer(result =>
            {
                if (result is UnityEventHandlerFindResult fr)
                {
                    results.Add(fr);
                }
                return FindExecution.Continue;
            }), SearchPattern.FIND_USAGES, subProgress);

            return results;
        }

        public override void Rename(IRenameRefactoring executer, IProgressIndicator pi, bool hasConflictsWithDeclarations,
                                    IRefactoringDriver driver)
        {
            if (myElementsToRename == null)
                return;
                    
            var workflow = (executer.Workflow as RenameWorkflowBase).NotNull("workflow != null");

            var persistentIndexManager = mySolution.GetComponent<IPersistentIndexManager>();
            foreach (var textOccurrence in myElementsToRename)
            {
                var sourceFile = persistentIndexManager[textOccurrence.MethodUsages.TextRangeOwner];
                if (sourceFile == null)
                    continue;
                workflow.DataModel.AddExtraTextOccurrence(new AssetTextOccurrence(textOccurrence, sourceFile, OldName, NewName, myIsProperty));
            }
        }

        public override IDeclaredElement NewDeclaredElement => myPointer.FindDeclaredElement();
        public override string NewName { get; }
        public override string OldName { get; }
        public override IDeclaredElement PrimaryDeclaredElement => myPointer.FindDeclaredElement();
        public override IList<IDeclaredElement> SecondaryDeclaredElements => null;
        
        private class AssetTextOccurrence : ITextOccurrenceRenameMarker
        {
            private readonly UnityEventHandlerOccurrence myAssetOccurrence;
            private readonly RangeMarker myRangeMarker;

            public AssetTextOccurrence(UnityEventHandlerOccurrence assetOccurrence, IPsiSourceFile sourceFile,
                string oldName, string newName, bool isProperty)
            {
                var curRange = assetOccurrence.MethodUsages.TextRangeOwnerPsiPersistentIndex;
                
                var pointer = sourceFile.Document.ToPointer();
                myRangeMarker =  new RangeMarker(pointer, isProperty ? new TextRange(curRange.StartOffset + 4, curRange.EndOffset) : curRange);
                myAssetOccurrence = assetOccurrence;
                NewName = newName;
                OldName = isProperty ? oldName.RemoveStart("get_").RemoveStart("set_") : oldName;
            }
            
            public bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
                TabOptions tabOptions = TabOptions.Default)
            {
                return myAssetOccurrence.Navigate(solution, windowContext, transferFocus, tabOptions);
            }

            public ISolution GetSolution() => myAssetOccurrence.GetSolution();

            public string DumpToString() => myAssetOccurrence.DumpToString();

            public OccurrenceType OccurrenceType => myAssetOccurrence.OccurrenceType;
            public bool IsValid => myAssetOccurrence.IsValid;
            
            public OccurrencePresentationOptions PresentationOptions { get; set; }
            public IDocument GetDocument() => myAssetOccurrence.SourceFile.Document;

            public string NewName { get; set; }

            public bool Included
            {
                get => true;
                set { }
            }

            public string OldName { get; }
            public TextRange OldNameRange => myRangeMarker.Range;
        }
    }
    
}