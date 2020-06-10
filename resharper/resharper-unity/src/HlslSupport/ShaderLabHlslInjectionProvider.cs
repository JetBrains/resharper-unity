using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Injections;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
{
    [SolutionComponent]
    public class ShaderLabHlslInjectionProvider : CppInjectionProviderBase
    {
        public override bool IsApplicable(PsiLanguageType originalLanguage)
        {
            return originalLanguage.Is<ShaderLabLanguage>();
        }

        public override bool IsApplicableToNode(ITreeNode node, IInjectedFileContext context)
        {
            return node is ICgContent;
        }

        public override IInjectedNodeContext Regenerate(IndependentInjectedNodeContext nodeContext)
        {
            var text = nodeContext.GeneratedNode.GetText();
            var token = ShaderLabTokenType.CG_CONTENT.CreateLeafElement(text);
            var content = new CgContent();
            content.AppendNewChild(token);

            var replacedNode = ModificationUtil.ReplaceChild(nodeContext.OriginalContextNode, content);
            return UpdateInjectedFileAndContext(nodeContext, replacedNode, 0, text.Length);
        }

        protected override CppFileLocation GetFileLocation(IPsiSourceFile sourceFile, ITreeNode originalNode)
        {
            var cppFileLocation = new CppFileLocation(sourceFile, originalNode.GetDocumentRange().TextRange);
            sourceFile.GetSolution().GetComponent<ShaderLabCppFileLocationTracker>()
                .VerifyLocation(sourceFile, cppFileLocation);
            return cppFileLocation;
        }


        protected override bool CanBeOriginalNode(ITreeNode node)
        {
            return node is ICgContent;
        }
    }
}