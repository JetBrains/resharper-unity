using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi
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

        public override IInjectedNodeContext Regenerate(IndependentInjectedNodeContext nodeContext)
        {
            var text = nodeContext.GeneratedNode.GetText();
            var parsedCg = CreateContent(nodeContext.OriginalContextNode.GetPsiModule(), text);
            var replacedNode = ModificationUtil.ReplaceChild(nodeContext.OriginalContextNode, parsedCg);
            return UpdateInjectedFileAndContext(nodeContext, replacedNode, 0, text.Length);
        }

        private ICgFile CreateContent(IPsiModule module, string text)
        {
            var generatedLexer = new CgLexerGenerated(new StringBuffer(text));
            var file = new CgParser(generatedLexer.ToCachingLexer(), myIntern).ParseFile();
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