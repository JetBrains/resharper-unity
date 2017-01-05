using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Gen;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Tree.Impl;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
{
    internal class ShaderLabParser : ShaderLabParserGenerated, IParser
    {
        private ITokenIntern myTokenIntern;

        public ShaderLabParser([NotNull] ILexer<int> lexer)
        {
            setLexer(new ShaderLabFilteringLexer(lexer));
        }

        public IFile ParseFile()
        {
            var element = ParseShaderLabFile();
            // TODO: Insert filtered tokens
            return (IFile) element;
        }

        //public override TreeElement ParseIdentifier()
        //{
        //    ParseIdent(result);


        //    var identifier = new Identifier(TokenIntern.Intern(myLexer));
        //    SetOffset(identifier, myLexer.TokenStart);
        //    myLexer.Advance();
        //    return identifier;
        //}

        //private ITokenIntern TokenIntern => myTokenIntern ?? (myTokenIntern = new LexerTokenIntern(10));
    }
}