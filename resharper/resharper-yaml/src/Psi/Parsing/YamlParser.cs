using JetBrains.DataFlow;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  internal class YamlParser : IYamlParser
  {
    private readonly ILexer<int> myLexer;

    public YamlParser(ILexer<int> lexer)
    {
      myLexer = lexer;
    }

    public IFile ParseFile()
    {
      using (var lifetimeDefinition = Lifetimes.Define())
      {
        var builder = CreateTreeBuilder(lifetimeDefinition.Lifetime);
        builder.ParseFile();
        return (IFile) builder.GetTree();
      }
    }

    public INode ParseRootBlockNode()
    {
      using (var lifetimeDefinition = Lifetimes.Define())
      {
        var builder = CreateTreeBuilder(lifetimeDefinition.Lifetime);
        builder.ParseRootBlockNode();

        var rootBlockNode = builder.GetTree();
        Assertion.Assert(rootBlockNode is INode, "rootBlockNode is INode");
        return (INode) rootBlockNode;
      }
    }

    private YamlTreeStructureBuilder CreateTreeBuilder(Lifetime lifetime)
    {
      return new YamlTreeStructureBuilder(myLexer, lifetime);
    }
  }
}