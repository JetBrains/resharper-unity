using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    public class UnityEventTargetAtomicRename : AtomicRenameBase
    {
        private readonly IDeclaredElementPointer<IDeclaredElement> myPointer;

        public UnityEventTargetAtomicRename(IDeclaredElement declaredElement, string newName)
        {
            myPointer = declaredElement.CreateElementPointer();
            OldName = declaredElement.ShortName;
            NewName = newName;
        }

        public override IRefactoringPage CreateRenamesConfirmationPage(IRenameWorkflow renameWorkflow, IProgressIndicator pi)
        {
            return new UnityEventTargetRefactoringPage(
                ((RefactoringWorkflowBase) renameWorkflow).WorkflowExecuterLifetime);
        }

        // ReSharper disable once IdentifierTypo
        public override void Rename(IRenameRefactoring executer, IProgressIndicator pi, bool hasConflictsWithDeclarations,
                                    IRefactoringDriver driver)
        {
            // Do nothing. We just want to hook into the UI process
        }

        public override IDeclaredElement NewDeclaredElement => myPointer.FindDeclaredElement();
        public override string NewName { get; }
        public override string OldName { get; }
        public override IDeclaredElement PrimaryDeclaredElement => myPointer.FindDeclaredElement();
        public override IList<IDeclaredElement> SecondaryDeclaredElements => null;
    }
}