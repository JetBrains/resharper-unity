#nullable enable

using JetBrains.DocumentModel;
using JetBrains.IDE.UI;
using JetBrains.ReSharper.Feature.Services.Breadcrumbs;
using JetBrains.ReSharper.Plugins.Unity.Services.Breadcrumbs;
using JetBrains.ReSharper.Plugins.Unity.Services.Tree;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.Breadcrumbs
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabBreadcrumbsProvider : StructuralDeclarationBreadcrumbsProviderBase
    {
        public ShaderLabBreadcrumbsProvider(IIconHost iconHost, PsiIconManager iconManager) : base(iconHost, iconManager) { }

        protected override bool CanGoToFileMember(IStructuralDeclaration declaration) => true;
        protected override bool CanShowFileStructure(IStructuralDeclaration declaration) => true;

        protected override void CollectNodeBreadcrumbs(ITreeNode node, IStructuralDeclaration? declaration, DocumentOffset documentOffset, ref LocalList<CrumbModel> crumbs)
        {
            if (declaration is ICodeBlock)
                CollectInjectedHlslCrumbs(ref crumbs, node, documentOffset);
        }

        private void CollectInjectedHlslCrumbs(ref LocalList<CrumbModel> crumbs, ITreeNode node, DocumentOffset documentOffset)
        {
            if (node.GetSourceFile() is { } sourceFile 
                && sourceFile.GetPsiFile<CppLanguage>(documentOffset) is {} cppFile 
                && LanguageManager.Instance.TryGetCachedService<ILanguageSpecificBreadcrumbsProvider>(cppFile.Language) is {} cgBlockBreadcrumbsProvider)
            {
                crumbs.AddRange(cgBlockBreadcrumbsProvider.CollectBreadcrumbs(cppFile, documentOffset));
            }
        }
    }
}
