using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Gen;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing
{
    internal class JsonNewParser : IParser
    {
        [NotNull]
        private readonly ILexer<int> myLexer;
        
        public JsonNewParser(ILexer<int> lexer)
        {
            myLexer = lexer;
        }

        public IFile ParseFile()
        {
            return Lifetime.Using(lifetime =>
            {
                var builder = CreateTreeBuilder(lifetime);
                builder.ParseFile();
                return (IFile) builder.GetTree();
            });
        }
        
        private JsonNewTreeBuilder CreateTreeBuilder(Lifetime lifetime)
        {
            return new JsonNewTreeBuilder(myLexer, lifetime);
        }

        public IJsonNewLiteralExpression ParseLiteral()
        {
            return Lifetime.Using(lifetime =>
            {
                var builder = CreateTreeBuilder(lifetime);
                builder.ParseJsonLiteralExpression();
                return (IJsonNewLiteralExpression) builder.GetTree();
            });
        }
    }
}