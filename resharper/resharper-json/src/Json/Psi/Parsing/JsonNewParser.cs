using JetBrains.Annotations;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing
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

        public IJsonNewValue ParseValue()
        {
            return Lifetime.Using(lifetime =>
            {
                var builder = CreateTreeBuilder(lifetime);
                builder.ParseJsonValue();
                return (IJsonNewValue) builder.GetTree();
            });
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