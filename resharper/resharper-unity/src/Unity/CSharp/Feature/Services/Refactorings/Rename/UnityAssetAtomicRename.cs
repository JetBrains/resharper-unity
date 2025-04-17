using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.DeferredCaches;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Refactorings.Rename;
using JetBrains.Util;
using JetBrains.Util.Extension;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    public class UnityAssetAtomicRename : AtomicRenameBase
    {
        private readonly ISolution mySolution;
        private readonly IDeclaredElementPointer<IDeclaredElement> myPointer;
        private IList<IAssetOccurrenceWithTextOccurrence> myElementsToRename;
        private bool myIsProperty;

        public UnityAssetAtomicRename(ISolution solution,
                                            IDeclaredElement declaredElement,
                                            string newName)
        {
            mySolution = solution;
            myPointer = declaredElement.CreateElementPointer();
            OldName = declaredElement.ShortName;
            NewName = newName;

            myElementsToRename = EmptyList<IAssetOccurrenceWithTextOccurrence>.InstanceList;
        }

        public override void PrepareToRename(IRenameRefactoring executer,
                                             IProgressIndicator pi,
                                             bool hasConflictsWithDeclarations,
                                             IRefactoringDriver driver)
        {
            // do not run find usages too, silent == true for player projects and misc project(misc could happen due to RIDER-53753)
            // If we run find usages for player/misc declared element, find usages will return empty result, because
            // guid is resolved to psiSourceFile from real projects only (GetTypeElementFromScriptAssetGuid in AssetUtils)

            // NOTE: find usages under the hood uses cache which stores TextRanges, cache will not be updated between several atomic renames.
            // That means that only one atomic rename should exist or only one atomic rename should return non-empty result from find usages below

            var de = myPointer.FindDeclaredElement();
            if (de == null) return;

            myIsProperty = de is IProperty;

            pi.Start(1);

            using var subProgress = pi.CreateSubProgress();
            myElementsToRename = GetAssetOccurrence(de, subProgress);
        }

        public override IRefactoringPage CreateRenamesConfirmationPage(IRenameWorkflow renameWorkflow, IProgressIndicator pi)
        {
            return new UnityAssetRefactoringPage(
                ((RefactoringWorkflowBase) renameWorkflow).WorkflowExecuterLifetime, mySolution.GetComponent<DeferredCacheController>());
        }

        private List<IAssetOccurrenceWithTextOccurrence> GetAssetOccurrence(IDeclaredElement de, IProgressIndicator subProgress)
        {
            var finder = mySolution.GetPsiServices().AsyncFinder;
            var occurrenceFactory = mySolution.GetComponent<OccurrenceFactory>();
            var module = mySolution.GetComponent<UnityExternalFilesModuleFactory>().PsiModule;
            var searchDomain = SearchDomainFactory.Instance.CreateSearchDomain(module);
            var results = new List<IAssetOccurrenceWithTextOccurrence>();

            var elements = de is IProperty property ? new[] {de, property.Getter, property.Setter} : new[] {de};

            finder.Find(elements.Where(t => t != null).ToArray(), searchDomain, new FindResultConsumer(result =>
            {
                var occurence = occurrenceFactory.MakeOccurrence(result);
                if (occurence is IAssetOccurrenceWithTextOccurrence assetOccurrenceWithTextOccurrence)
                    results.Add(assetOccurrenceWithTextOccurrence);
                return FindExecution.Continue;
            }), SearchPattern.FIND_USAGES, subProgress);

            return results;
        }

        public override void Rename(IRenameRefactoring executer, IProgressIndicator pi, bool hasConflictsWithDeclarations,
                                    IRefactoringDriver driver, PreviousAtomicRenames previousAtomicRenames)
        {
            if (myElementsToRename.IsEmpty())
                return;

            var workflow = (executer.Workflow as RenameWorkflowBase).NotNull("workflow != null");

            foreach (var textOccurrence in myElementsToRename)
            {
                var sourceFile = textOccurrence.GetSourceFile();
                if (sourceFile == null)
                    continue;
                workflow.DataModel.AddExtraTextOccurrence(new AssetTextOccurrence(textOccurrence, sourceFile, OldName, NewName, myIsProperty));
            }
        }

        public override IDeclaredElement NewDeclaredElement =>
            myPointer.FindDeclaredElement().NotNull("myPointer.FindDeclaredElement() != null");

        public override string NewName { get; }
        public override string OldName { get; }

        public override IDeclaredElement PrimaryDeclaredElement =>
            myPointer.FindDeclaredElement().NotNull("myPointer.FindDeclaredElement() != null");

        public override IList<IDeclaredElement>? SecondaryDeclaredElements => null;

        private class AssetTextOccurrence : ITextOccurrenceRenameMarker
        {
            private readonly IAssetOccurrenceWithTextOccurrence myAssetOccurrence;
            private readonly RangeMarker myRangeMarker;

            public AssetTextOccurrence(IAssetOccurrenceWithTextOccurrence assetOccurrence, IPsiSourceFile sourceFile,
                string oldName, string newName, bool isProperty)
            {
                var curRange = assetOccurrence.RenameTextRange;

                myRangeMarker =  new RangeMarker(sourceFile.Document, isProperty ? new TextRange(curRange.StartOffset + 4, curRange.EndOffset) : curRange);
                myAssetOccurrence = assetOccurrence;
                NewName = newName;
                OldName = isProperty ? oldName.RemoveStart("get_").RemoveStart("set_") : oldName;
            }

            public bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
                TabOptions tabOptions = TabOptions.Default)
            {
                return myAssetOccurrence.Navigate(solution, windowContext, transferFocus, tabOptions);
            }

            public ISolution? GetSolution() => myAssetOccurrence.GetSolution();

            public string DumpToString() => myAssetOccurrence.DumpToString();

            public OccurrenceType OccurrenceType => myAssetOccurrence.OccurrenceType;
            public bool IsValid => myAssetOccurrence.IsValid;

            public OccurrencePresentationOptions PresentationOptions { get; set; }
            public IDocument GetDocument() => myAssetOccurrence.GetSourceFile().Document;

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