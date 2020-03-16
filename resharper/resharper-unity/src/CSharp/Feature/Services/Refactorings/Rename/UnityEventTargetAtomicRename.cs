using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Util;

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
                    .Select(t =>
                    {
                        var curRange = t.AssetMethodData.TextRange;

                        var range = de is IProperty ? new TextRange(curRange.StartOffset + 4, curRange.EndOffset) : curRange;  
                        return new TextOccurrenceRenameMarker(
                            new FindResultText(t.SourceFile, new DocumentRange(t.SourceFile.Document, range)), NewName);
                    }).ToList();
            }
            
            return new UnityEventTargetRefactoringPage(
                ((RefactoringWorkflowBase) renameWorkflow).WorkflowExecuterLifetime, mySolution.GetComponent<DeferredCacheController>());
        }

        private List<UnityMethodsFindResult> GetAssetOccurrence(IDeclaredElement de, IProgressIndicator subProgress)
        {
            var finder = mySolution.GetPsiServices().AsyncFinder;
            var module = mySolution.GetComponent<UnityExternalFilesModuleFactory>().PsiModule;
            var searchDomain = SearchDomainFactory.Instance.CreateSearchDomain(module);
            var results = new List<UnityMethodsFindResult>();

            var elements = de is IProperty property ? new[] {de, property.Getter, property.Setter} : new[] {de};
            
            finder.Find(elements.Where(t => t != null).ToArray(), searchDomain, new FindResultConsumer(result =>
            {
                if (result is UnityMethodsFindResult fr)
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
            if (myElementsToRename == null)
                return;
            
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