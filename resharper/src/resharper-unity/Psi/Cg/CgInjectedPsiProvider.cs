using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing;
using JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Tree;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg
{
    [SolutionComponent]
    public class CgInjectedPsiProvider : IndependentInjectedPsiProvider
    {
        private readonly CommonIdentifierIntern myIntern;

        public CgInjectedPsiProvider(CommonIdentifierIntern intern)
        {
            myIntern = intern;
        }

        public override bool ProvidedLanguageCanHaveNestedInjects => false;

        public override PsiLanguageType GeneratedLanguage => (PsiLanguageType) CgLanguage.Instance ?? UnknownLanguage.Instance;
        
        public override bool IsApplicable(PsiLanguageType originalLanguage)
        {
            return originalLanguage.Is<ShaderLabLanguage>();
        }

        public override bool IsApplicableToNode(ITreeNode node, IInjectedFileContext context)
        {
            return node is ICgContent;
        }

        public override IInjectedNodeContext CreateInjectedNodeContext(IInjectedFileContext fileContext, ITreeNode originalNode)
        {
            var text = originalNode.GetText();
            if (string.IsNullOrEmpty(text))
                return null;
            var stringBuffer = new StringBuffer(text);
            var languageService = CgLanguage.Instance.LanguageService();
            if (languageService == null)
                return null;
            return CreateInjectedFileAndContext(fileContext, originalNode, stringBuffer, languageService, 0, text.Length, 0, text.Length);
        }

        public override void Regenerate(IndependentInjectedNodeContext nodeContext)
        {
            var text = nodeContext.GeneratedNode.GetText();
            var parsedCg = CreateContent(nodeContext.OriginalContextNode.GetPsiModule(), text);
            var replacedNode = ModificationUtil.ReplaceChild(nodeContext.OriginalContextNode, parsedCg);
            UpdateInjectedFileAndContext(nodeContext, replacedNode, 0, text.Length);
        }

        private ICgFile CreateContent(IPsiModule module, string text)
        {
            var file = new CgParser(new CgLexerGenerated(new StringBuffer(text)), myIntern).ParseFile();
            if (file == null)
                throw new ElementFactoryException("Cannot create IFile");
            SandBox.CreateSandBoxFor(file, module);
            return file.Children().OfType<ICgFile>().First();
        }

        protected override bool CanBeGeneratedNode(ITreeNode node)
        {
            // TODO: not sure about that
            return node is ICgFile;
        }

        protected override bool CanBeOriginalNode(ITreeNode node)
        {
            return node is ICgContent;
        }
    }
}