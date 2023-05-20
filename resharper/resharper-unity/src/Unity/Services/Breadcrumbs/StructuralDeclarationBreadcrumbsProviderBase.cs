#nullable enable

using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.IDE.UI;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Feature.Services.Breadcrumbs;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.Services.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Services.Breadcrumbs
{
    public abstract class StructuralDeclarationBreadcrumbsProviderBase : ILanguageSpecificBreadcrumbsProvider
    {
        private readonly IIconHost myIconHost;
        private readonly PsiIconManager myIconManager;

        protected virtual bool CanGoToFileMember(IStructuralDeclaration declaration) => true;
        protected virtual bool CanShowFileStructure(IStructuralDeclaration declaration) => true;

        protected StructuralDeclarationBreadcrumbsProviderBase(IIconHost iconHost, PsiIconManager iconManager)
        {
            myIconHost = iconHost;
            myIconManager = iconManager;
        }

        private void CollectBreadcrumbs(IPsiSourceFile sourceFile, IStructuralDeclaration declaration, ref LocalList<CrumbModel> crumbs)
        {
            if (declaration.DeclaredElement is not {} targetElement)
                return;

            if (declaration.ContainingDeclaration is { } containingDeclaration)
                CollectBreadcrumbs(sourceFile, containingDeclaration, ref crumbs);

            Interruption.Current.CheckAndThrow();

            crumbs.Add(CreateCrumbModel(sourceFile, declaration, targetElement));
        }

        private CrumbModel CreateCrumbModel(IPsiSourceFile sourceFile, IStructuralDeclaration declaration, IDeclaredElement targetElement)
        {
            var language = declaration.Language;
            var presentation = DeclaredElementPresenter.Format(language, DeclaredElementPresenter.QUALIFIED_NAME_PRESENTER, targetElement);
            var icon = myIconManager.GetImage(targetElement, language, true);
            var textRange = declaration.GetDocumentRange().TextRange;
            var rdRange = new RdTextRange(textRange.StartOffset, textRange.EndOffset);
            var actions = new List<CrumbAction>();
            if (CanGoToFileMember(declaration))
                actions.Add(new CrumbAction(Strings.GoToFileMembers_Text, "FileStructurePopup"));
            if (CanShowFileStructure(declaration))
                actions.Add(new CrumbAction(Strings.ShowFileStructure_Text, "Structure"));
            
            var crumbModel = new CrumbModel(targetElement.ShortName, $"{presentation}\n{Strings.ClickToNavigate_Text}", myIconHost.Transform(icon), rdRange, actions);
            var elementPointer = targetElement.CreateElementPointer();
            crumbModel.Navigate.SetVoid(_ => NavigateTo(sourceFile, elementPointer));
            return crumbModel;
        }

        private void NavigateTo(IPsiSourceFile sourceFile, IDeclaredElementPointer<IDeclaredElement> elementPointer)
        {
            using (ReadLockCookie.Create())
            {
                if (elementPointer.FindDeclaredElement() is not {} declaredElement)
                    return;
                declaredElement.GetPsiServices().Files.ExecuteAfterCommitAllDocuments(() =>
                {
                    if (!declaredElement.IsValid())
                        return;
                    using (CompilationContextCookie.GetOrCreate(sourceFile.ResolveContext))
                        declaredElement.Navigate(false);
                });
            }
        }

        /// <summary>Automatically collects breadcrumbs for <see cref="IStructuralDeclaration"/> nodes and lets the implementation to add custom breadcrumbs for current node with <see cref="CollectNodeBreadcrumbs"/>.</summary>
        public IEnumerable<CrumbModel> CollectBreadcrumbs(IFile psiFile, DocumentOffset documentOffset)
        {
            if (psiFile.GetSourceFile() is not { } sourceFile || psiFile.FindNodeAt(documentOffset) is not {} currentNode)
                return EmptyList<CrumbModel>.Instance;
            
            var crumbs = new LocalList<CrumbModel>();
            var currentDeclaration = currentNode.GetContainingNode<IStructuralDeclaration>();
            if (currentDeclaration != null)
                CollectBreadcrumbs(sourceFile, currentDeclaration, ref crumbs);

            Interruption.Current.CheckAndThrow();
            CollectNodeBreadcrumbs(currentNode, currentDeclaration, documentOffset, ref crumbs);

            return crumbs.ReadOnlyList();
        }

        protected virtual void CollectNodeBreadcrumbs(ITreeNode node, IStructuralDeclaration? declaration, DocumentOffset documentOffset, ref LocalList<CrumbModel> crumbs)
        {
        }
    }
}
