#nullable enable

using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.IDE.UI;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Feature.Services.Breadcrumbs;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.Navigation.NavigationProviders;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.UI.Icons;

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
            var solution = psiFile.GetSolution();
            var crumbs = new Stack<CrumbModel>();
            var currentNode = psiFile.FindNodeAt(documentOffset);
            foreach (var containingNode in currentNode.ContainingNodes(true))
            {
                switch (containingNode)
                {
                    case ICgContent: 
                        CollectInjectedHlslCrumbs(crumbs, containingNode, documentOffset);
                        break;
                    case IIncludeBlock includeBlock:
                        AddCrumb(crumbs, solution, includeBlock.StartDelimiter);
                        break;
                    case IProgramBlock programBlock:
                        AddCrumb(crumbs, solution, programBlock.Program);
                        break;
                    case IBlockCommand command:
                        AddCrumb(crumbs, solution, command.CommandKeyword);
                        break;
                }
            }

            return crumbs;
        }

        private void AddCrumb(Stack<CrumbModel> crumbs, ISolution solution, ITreeNode node)
        {
            Interruption.Current.CheckAndThrow();
            
            var icon = default(IconId);
            var documentRange = node.GetDocumentRange();
            var rdRange = new RdTextRange(documentRange.TextRange.StartOffset, documentRange.TextRange.EndOffset);
            var nodeName = node.NodeType is ITokenNodeType tokenNodeType ? tokenNodeType.TokenRepresentation : node.NodeType.ToString();
            var crumbModel = new CrumbModel(nodeName , $"{nodeName}\n{Strings.ClickToNavigate_Text}", myIconHost.Transform(icon), rdRange, new List<CrumbAction>());
            
            crumbModel.Navigate.SetVoid(_ =>
            {
                using (ReadLockCookie.Create())
                {
                    solution.GetPsiServices().Files.ExecuteAfterCommitAllDocuments(() =>
                    {
                        if (node.IsValid() && node.GetContainingFile() is { } file && file.GetSourceFile()?.ToProjectFile() is { } projectFile)
                            NavigationManager.GetInstance(solution).Navigate<IProjectFileTextRangeNavigationProvider, ProjectFileTextRange>(new ProjectFileTextRange(projectFile, file.GetDocumentRange(node.GetTreeTextRange()).TextRange, null), true);
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
