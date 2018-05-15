using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Plugins.Unity.Json.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.Services;
using JetBrains.ReSharper.Psi.Pointers;
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

            var factory = JavaScriptElementFactory.GetInstance(originalTreeNode);
            var literalExpression = factory.CreateExpression("\"$0\"", NewName);
            var newExpression = originalTreeNode.ReplaceBy(literalExpression);

            var references = executer.Workflow.GetElementReferences(declaredElement);
            if (!references.Any())
                return;

            pi.Start(references.Count);

            // Create a new declared element (other implementations don't appear to cache this)
            var element = new AsmDefNameDeclaredElement(declaredElement.GetJsServices(), NewName,
                declaredElement.SourceFile, newExpression.GetTreeStartOffset().Offset);

            // Rename/bind the references
            foreach (var pair in LanguageUtil.SortReferences(references.Where(r => r.IsValid())))
            {
                foreach (var sortedReference in LanguageUtil.GetSortedReferences(pair.Value))
                {
                    InterruptableActivityCookie.CheckAndThrow(pi);

                    if (sortedReference.IsValid())
                        sortedReference.BindTo(element);
                    pi.Advance();
                }
            }

            element.GetPsiServices().Caches.Update();
            myNewPointer = element.CreateElementPointer();
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