#nullable enable
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Refactorings.Rename;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.Refactorings.Rename
{
    public class ShaderLabAtomicRename : AtomicRenameBase
    {
        private readonly IDeclaredElementPointer<IDeclaredElement> myElementPointer;
        private IDeclaredElementPointer<IDeclaredElement>? myNewElementPointer;

        public override IDeclaredElement? PrimaryDeclaredElement => myElementPointer.FindDeclaredElement();
        public override IList<IDeclaredElement>? SecondaryDeclaredElements => null;
        public override IDeclaredElement NewDeclaredElement => (myNewElementPointer?.FindDeclaredElement()).NotNull();
        public override string OldName { get; }
        public override string NewName { get; }

        public ShaderLabAtomicRename(IDeclaredElement element, string newName)
        {
            myElementPointer = element.CreateElementPointer();
            OldName = element.ShortName;
            NewName = newName;
        }

        public override void Rename(
            IRenameRefactoring executer, IProgressIndicator pi, bool hasConflictsWithDeclarations,
            IRefactoringDriver driver, PreviousAtomicRenames previousAtomicRenames)
        {
            // Rename the "declaration"
            var declaredElement = myElementPointer.FindDeclaredElement();
            if (declaredElement?.GetDeclarations() is not { Count: > 0 } declarations)
                return;

            foreach (var declaration in declarations) 
                declaration.SetName(NewName);

            var element = declarations[0].DeclaredElement;
            var references = executer.Workflow.GetElementReferences(declaredElement);
            if (references.IsNullOrEmpty())
                return;

            pi.Start(references.Count);

            // Rename/bind the references
            foreach (var pair in LanguageUtil.SortReferences(references.Where(r => r.IsValid())))
            {
                foreach (var sortedReference in LanguageUtil.GetSortedReferences(pair.Value))
                {
                    Interruption.Current.CheckAndThrow();

                    if (sortedReference.IsValid())
                        sortedReference.BindTo(element);

                    pi.Advance();
                }
            }

            element.GetPsiServices().Caches.Update();
            myNewElementPointer = element.CreateElementPointer();
        }
    }
}