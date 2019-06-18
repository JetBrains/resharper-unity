using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl;
using JetBrains.ReSharper.Plugins.Yaml.Psi.UnityAsset;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public class UAParser : YamlParser
  {
    private readonly ILexer<int> myLexer;

    public UAParser(ILexer<int> lexer)
      : base(lexer)
    {
      myLexer = lexer;
    }

    public override IFile ParseFile()
    {
      var file = new UAFile();
      while (myLexer.TokenType != null)
      {
        var token = myLexer.TokenType.Create(myLexer.Buffer,
          new TreeOffset(myLexer.TokenStart),
          new TreeOffset(myLexer.TokenEnd));

        if (myLexer.TokenType == YamlTokenType.DOCUMENT)
        {
          var document = new UAChameleonDocument((ClosedChameleonElement) token);
          file.AddChild(document);
        }
        else
          file.AddChild(token);
        
        myLexer.Advance();
      }
      return file;
    }

  }
}