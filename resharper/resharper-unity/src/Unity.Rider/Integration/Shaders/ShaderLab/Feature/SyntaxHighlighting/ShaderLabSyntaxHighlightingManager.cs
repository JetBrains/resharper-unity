using JetBrains.ReSharper.Daemon.Syntax;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.ShaderLab.Feature.SyntaxHighlighting
{
    [Language(typeof (ShaderLabLanguage))]
    internal class ShaderLabSyntaxHighlightingManager : SyntaxHighlightingManager
    {
        public override SyntaxHighlightingProcessor CreateProcessor(IPsiSourceFile sourceFile, IFile psiFile)
        {
            return new ShaderLabSyntaxHighlightingProcessor();
        }
    }
}