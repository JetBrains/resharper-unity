using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Gen;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing
{
    internal class CgParser : CgParserGenerated, IParser
    {
        public CgParser([NotNull] ILexer lexer)
        {
            SetLexer(new CgFilteringLexer(lexer));
        }

        public IFile ParseFile()
        {
            return (IFile) ParseCgFile();
        }
    }
}