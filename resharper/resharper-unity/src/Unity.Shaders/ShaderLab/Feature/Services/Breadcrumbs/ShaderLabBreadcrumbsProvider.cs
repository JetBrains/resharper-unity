#nullable enable

using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.IDE.UI;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Feature.Services.Breadcrumbs;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.Breadcrumbs
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabBreadcrumbsProvider : ILanguageSpecificBreadcrumbsProvider
    {
        private readonly IIconHost myIconHost;
        private readonly PsiIconManager myIconManager;

        public ShaderLabBreadcrumbsProvider(IIconHost iconHost, PsiIconManager iconManager)
        {
            myIconHost = iconHost;
            myIconManager = iconManager;
        }

        public IEnumerable<CrumbModel> CollectBreadcrumbs(IFile psiFile, DocumentOffset documentOffset)
        {
            var crumbs = new Stack<CrumbModel>();
            var currentNode = psiFile.FindNodeAt(documentOffset);
            foreach (var containingNode in currentNode.ContainingNodes(true))
            {
                switch (containingNode)
                {
                    case ICgContent: 
                        CollectInjectedHlslCrumbs(crumbs, containingNode, documentOffset);
                        break;
                    case IDeclaration declaration and (IBlockCommand or IIncludeBlock or IProgramBlock):
                        AddCrumb(crumbs, declaration);
                        break;
                }
            }

            return crumbs;
        }
        
        private void AddCrumb(Stack<CrumbModel> crumbs, IDeclaration declaration)
        {
            Interruption.Current.CheckAndThrow();

            var targetElement = declaration.DeclaredElement;
            if (targetElement == null || declaration.GetContainingFile() is not {} file)
                return;
            
            var presentation = DeclaredElementPresenter.Format(ShaderLabLanguage.Instance, DeclaredElementPresenter.QUALIFIED_NAME_PRESENTER, targetElement);
            
            var icon = myIconManager.GetImage(targetElement, file.Language, true);
            var documentRange = file.GetDocumentRange(declaration.GetNameRange());
            var rdRange = new RdTextRange(documentRange.TextRange.StartOffset, documentRange.TextRange.EndOffset);
            var crumbModel = new CrumbModel(targetElement.ShortName , $"{presentation}\n{Strings.ClickToNavigate_Text}", myIconHost.Transform(icon), rdRange, new List<CrumbAction>());
            
            crumbModel.Navigate.SetVoid(_ =>
            {
                using (ReadLockCookie.Create())
                {
                    targetElement.GetPsiServices().Files.ExecuteAfterCommitAllDocuments(() =>
                    {
                        if (!file.IsValid())
                            return;
                        using (CompilationContextCookie.GetOrCreate(file.GetResolveContext()))
                            targetElement.Navigate(false);
                    });
                }
            });
            crumbs.Push(crumbModel);
        }
        
        private void CollectInjectedHlslCrumbs(Stack<CrumbModel> crumbs, ITreeNode node, DocumentOffset documentOffset)
        {
            if (node.GetSourceFile() is { } sourceFile 
                && sourceFile.GetPsiFile<CppLanguage>(documentOffset) is {} cppFile 
                && LanguageManager.Instance.TryGetCachedService<ILanguageSpecificBreadcrumbsProvider>(cppFile.Language) is {} cgBlockBreadcrumbsProvider)
            {
                foreach (var crumb in cgBlockBreadcrumbsProvider.CollectBreadcrumbs(cppFile, documentOffset).Reverse())
                    crumbs.Push(crumb);
            }
        }
    }
}
