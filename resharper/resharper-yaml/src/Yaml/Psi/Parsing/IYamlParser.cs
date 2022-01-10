using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
{
  public interface IYamlParser : IParser
  {
    IDocumentBody ParseDocumentBody();
  }
}