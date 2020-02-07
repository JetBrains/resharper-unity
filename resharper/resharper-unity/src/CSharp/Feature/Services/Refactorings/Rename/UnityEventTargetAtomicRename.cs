using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    public class UnityEventTargetAtomicRename : AtomicRenameBase
    {
        private readonly ISolution mySolution;
        private readonly IDeclaredElementPointer<IDeclaredElement> myPointer;
        private List<TextOccurrenceRenameMarker> myElementsToRename;
        public UnityEventTargetAtomicRename(ISolution solution, IDeclaredElement declaredElement, string newName)
        {
            mySolution = solution;
            myPointer = declaredElement.CreateElementPointer();
            OldName = declaredElement.ShortName;
            NewName = newName;
        }

        public override IRefactoringPage CreateRenamesConfirmationPage(IRenameWorkflow renameWorkflow, IProgressIndicator pi)
        {
            var de = myPointer.FindDeclaredElement();
            if (de == null)
                return null;
            
            pi.Start(1);
            using (var subProgress = pi.CreateSubProgress())
            {
                myElementsToRename = GetAssetOccurrence(de, subProgress)
                    .Select(t => new TextOccurrenceRenameMarker(
                        new FindResultText(t.SourceFile, new DocumentRange(t.SourceFile.Document, t.TextRange)),
                        NewName)).ToList();
            }
            
            return new UnityEventTargetRefactoringPage(
                ((RefactoringWorkflowBase) renameWorkflow).WorkflowExecuterLifetime);
        }

        private List<UnityAssetFindResult> GetAssetOccurrence(IDeclaredElement de, IProgressIndicator subProgress)
        {
            var finder = mySolution.GetPsiServices().AsyncFinder;
            var module = mySolution.GetComponent<UnityExternalFilesModuleFactory>().PsiModule;
            var searchDomain = SearchDomainFactory.Instance.CreateSearchDomain(module);
            var results = new List<UnityAssetFindResult>();
            finder.Find(new []{de}, searchDomain, new FindResultConsumer(result =>
            {
                if (result is UnityAssetFindResult fr)
                {
                    results.Add(fr);
                }
                return FindExecution.Continue;
            }), SearchPattern.FIND_USAGES, subProgress);

            return results;
        }

        // ReSharper disable once IdentifierTypo
        public override void Rename(IRenameRefactoring executer, IProgressIndicator pi, bool hasConflictsWithDeclarations,
                                    IRefactoringDriver driver)
        {
            foreach (var textOccurrence in myElementsToRename)
            {
                if (!textOccurrence.Included || !textOccurrence.Marker.IsValid) continue;
                var document = textOccurrence.GetDocument();

                var range = textOccurrence.Marker.DocumentRange.TextRange;
                if (range.IsValid)
                    document.ReplaceText(range, textOccurrence.NewName);
            }
        }

        public override IDeclaredElement NewDeclaredElement => myPointer.FindDeclaredElement();
        public override string NewName { get; }
        public override string OldName { get; }
        public override IDeclaredElement PrimaryDeclaredElement => myPointer.FindDeclaredElement();
        public override IList<IDeclaredElement> SecondaryDeclaredElements => null;
    }
}