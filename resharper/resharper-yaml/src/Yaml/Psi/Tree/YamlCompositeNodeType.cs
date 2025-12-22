using System;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Tree
{
  public abstract class YamlCompositeNodeType : CompositeNodeType
  {
    protected YamlCompositeNodeType(string s, int index, Type nodeType)
      : base(s, index, nodeType)
    {
    }
  }
}