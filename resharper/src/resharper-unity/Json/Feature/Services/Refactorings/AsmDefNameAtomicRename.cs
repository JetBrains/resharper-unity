using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Plugins.Unity.Json.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.Services;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Refactorings.Rename;
using JetBrains.Util;
using JetBrains.Util.Special;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.Refactorings
{
    public class AsmDefNameAtomicRename : AtomicRenameBase
    {
        private readonly IDeclaredElementPointer<AsmDefNameDeclaredElement> myPointer;
        private IDeclaredElementPointer<AsmDefNameDeclaredElement> myNewPointer;

        public AsmDefNameAtomicRename(AsmDefNameDeclaredElement declaredElement, string newName)
        {
            myPointer = declaredElement.CreateElementPointer();
            NewName = newName;
            OldName = declaredElement.ShortName;
        }

        public override void Rename(IRenameRefactoring executer, IProgressIndicator pi, bool hasConflictsWithDeclarations,
            IRefactoringDriver driver)
        {
            // Rename the "declaration"
            var declaredElement = myPointer.FindDeclaredElement();
            var originalTreeNode = declaredElement?.GetTreeNode();
            if (originalTreeNode == null)
                return;

            var originalRange = originalTreeNode.GetDocumentRange();
            var factory = JavaScriptElementFactory.GetInstance(originalTreeNode);
            var literalExpression = factory.CreateExpression("\"$0\"", NewName);
            var newExpression = originalTreeNode.ReplaceBy(literalExpression);

            RemoveFromTextualOccurrences(executer, originalRange);

            var references = executer.Workflow.GetElementReferences(declaredElement);
            if (!Enumerable.Any(references))
                return;

            pi.Start(references.Count);

            // Create a new declared element (other implementations don't appear to cache this, either)
            var element = new AsmDefNameDeclaredElement(declaredElement.GetJsServices(), NewName,
                declaredElement.SourceFile, newExpression.GetTreeStartOffset().Offset);

            // Rename/bind the references
            foreach (var pair in LanguageUtil.SortReferences(references.Where(r => r.IsValid())))
            {
                foreach (var sortedReference in LanguageUtil.GetSortedReferences(pair.Value))
                {
                    InterruptableActivityCookie.CheckAndThrow(pi);

                    var referenceRange = sortedReference.GetDocumentRange();

                    if (sortedReference.IsValid())
                        sortedReference.BindTo(element);

                    RemoveFromTextualOccurrences(executer, referenceRange);

                    pi.Advance();
                }
            }

            element.GetPsiServices().Caches.Update();
            myNewPointer = element.CreateElementPointer();
        }

        private static void RemoveFromTextualOccurrences(IRenameRefactoring executer, DocumentRange handledRange)
        {
            if (!(executer.Workflow is RenameWorkflow workflow))
                return;

            foreach (var occurrence in workflow.DataModel.ActualOccurrences ?? EmptyList<TextOccurrenceRenameMarker>.InstanceList)
            {
                if (!occurrence.Included)
                    continue;

                var occurrenceRange = occurrence.Marker.DocumentRange;
                if (handledRange.Contains(occurrenceRange))
                {
                    occurrence.Included = false;
                    break;
                }
            }
        }

        public override IDeclaredElement NewDeclaredElement
        {
            get { return myNewPointer.IfNotNull(p => p.FindDeclaredElement()); }
        }

        public override string NewName { get; }
        public override string OldName { get; }
        public override IDeclaredElement PrimaryDeclaredElement => myPointer.FindDeclaredElement();
        public override IList<IDeclaredElement> SecondaryDeclaredElements => null;
    }
}