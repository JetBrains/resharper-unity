using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Parsing
{
    public class UnityYamlParser : YamlParser
    {
        private readonly ILexer<int> myLexer;

        public UnityYamlParser(ILexer<int> lexer)
            : base(lexer)
        {
            myLexer = lexer;
        }

        public override IFile ParseFile()
        {
            var file = new UnityYamlFile();
            while (myLexer.TokenType != null)
            {
                var token = myLexer.TokenType.Create(myLexer.Buffer,
                    new TreeOffset(myLexer.TokenStart),
                    new TreeOffset(myLexer.TokenEnd));

                if (myLexer.TokenType == UnityYamlTokenType.DOCUMENT)
                {
                    var document = new UnityYamlChameleonDocument((ClosedChameleonElement) token);
                    file.AddChild(document);
                }
                else
                {
                    file.AddChild(token);
                    file.AddComponentDocument(token);
                }

                myLexer.Advance();
            }

            return file;
        }
    }
}