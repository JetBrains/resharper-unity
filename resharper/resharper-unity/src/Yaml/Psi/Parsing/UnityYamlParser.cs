using System;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
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
            throw new InvalidOperationException("This method should be never called");
        }
    }
}