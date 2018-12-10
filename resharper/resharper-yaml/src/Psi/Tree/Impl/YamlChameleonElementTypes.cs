using System;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.TreeBuilder;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl
{
  public static class YamlChameleonElementTypes
  {
    // ReSharper disable once InconsistentNaming
    private sealed class CHAMELEON_DOCUMENT_BODY_INTERNAL : CompositeNodeType
    {
      public CHAMELEON_DOCUMENT_BODY_INTERNAL()
        : base("CHAMELEON_DOCUMENT_BODY", CHAMELEON_DOCUMENT_BODY_INDEX)
      {
      }

      public override CompositeElement Create() => new ChameleonDocumentBody();
    }

    public static readonly CompositeNodeType CHAMELEON_DOCUMENT_BODY = new CHAMELEON_DOCUMENT_BODY_INTERNAL();

    // ReSharper disable once InconsistentNaming
    private sealed class CHAMELEON_BLOCK_MAPPING_NODE_INTERNAL : CompositeNodeWithArgumentType
    {
      public CHAMELEON_BLOCK_MAPPING_NODE_INTERNAL()
        : base("CHAMELEON_BLOCK_MAPPING_NODE", CHAMELEON_BLOCK_MAPPING_NODE_INDEX)
      {
      }

      public override CompositeElement Create() =>
        throw new InvalidOperationException("Cannot create chameleon node without context");

      public override CompositeElement Create(object userData)
      {
        return new ChameleonBlockMappingNode();
      }
    }

    public static readonly CompositeNodeType CHAMELEON_BLOCK_MAPPING_NODE = new CHAMELEON_BLOCK_MAPPING_NODE_INTERNAL();

    // Tokens start at 1000, generated elements at 2000, so let's just use 3000
    public const int CHAMELEON_DOCUMENT_BODY_INDEX = 3000;
    public const int CHAMELEON_BLOCK_MAPPING_NODE_INDEX = 3001;
  }
}
