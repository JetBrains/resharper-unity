using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public class YamlParser : IYamlParser
  {
    private readonly ILexer<int> myLexer;

    public YamlParser(ILexer<int> lexer)
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

    public IDocumentBody ParseDocumentBody()
    {
      return Lifetime.Using(lifetime =>
      {
        var builder = CreateTreeBuilder(lifetime);
        builder.ParseDocumentBody();
        return (IDocumentBody) builder.GetTree();
      });
    }

    public INode ParseRootBlockNode()
    {
      return Lifetime.Using(lifetime =>
      {
        var builder = CreateTreeBuilder(lifetime);
        builder.ParseRootBlockNode();

        var rootBlockNode = builder.GetTree();
        Assertion.Assert(rootBlockNode is INode, "rootBlockNode is INode");
        return (INode) rootBlockNode;
      });
    }

    private YamlTreeStructureBuilder CreateTreeBuilder(Lifetime lifetime)
    {
      return new YamlTreeStructureBuilder(myLexer, lifetime);
    }
  }
}