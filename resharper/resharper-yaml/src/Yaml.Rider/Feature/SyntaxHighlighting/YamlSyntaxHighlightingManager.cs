using JetBrains.ReSharper.Daemon.Syntax;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Yaml.Rider.Feature.SyntaxHighlighting
{
    // Override RiderSyntaxHighlightingManager (registered for all known languages) which provides simple syntax
    // highlighting based on token node types (string literal, keyword, comment, number, etc.). We don't want any
    // syntax highlighting, as it's handled by the frontend
    [Language(typeof(YamlLanguage))]
    internal class YamlSyntaxHighlightingManager : SyntaxHighlightingManager
    {
        public override SyntaxHighlightingProcessor CreateProcessor(IPsiSourceFile sourceFile, IFile psiFile)
        {
          return null;
        }
    }
}
