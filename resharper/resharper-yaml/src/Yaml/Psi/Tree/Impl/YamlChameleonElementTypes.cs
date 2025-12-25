using System;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.TreeBuilder;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl
{
  partial class ElementType
  {
    // ReSharper disable once InconsistentNaming
    private sealed class CHAMELEON_DOCUMENT_BODY_INTERNAL : CompositeNodeType
    {
      public CHAMELEON_DOCUMENT_BODY_INTERNAL()
        : base("CHAMELEON_DOCUMENT_BODY", DOCUMENT_BODY_NODE_TYPE_INDEX, typeof(ChameleonDocumentBody))
      {
      }

      public override CompositeElement Create() => new ChameleonDocumentBody();
    }

    public static readonly CompositeNodeType CHAMELEON_DOCUMENT_BODY = new CHAMELEON_DOCUMENT_BODY_INTERNAL();
    
    
    private sealed class CHAMELEON_BLOCK_MAPPING_ENTRY_CONTENT : CompositeNodeWithArgumentType
    {
      public CHAMELEON_BLOCK_MAPPING_ENTRY_CONTENT() : base("CHAMELEON_MAP_ENTRY_CONTENT", CONTENT_NODE_NODE_TYPE_INDEX, typeof(ChameleonContentNode))
      {
      }

      public override CompositeElement Create() => throw new InvalidOperationException();
      public override CompositeElement Create(object message) => new ChameleonContentNode((ContentContext) message);
    }

    public static readonly CompositeNodeType MAP_VALUE_CHAMELEON = new CHAMELEON_BLOCK_MAPPING_ENTRY_CONTENT();
  }
}
