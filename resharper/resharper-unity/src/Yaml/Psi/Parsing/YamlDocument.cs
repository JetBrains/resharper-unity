#pragma warning disable 0168, 0219, 0108, 0414, 0114
// ReSharper disable RedundantNameQualifier

using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Parsing
{
  internal partial class YamlDocument
    : YamlCompositeElement, JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.IYamlDocument{
    public const short DIRECTIVES = ChildRole.LAST + 1;
    public const short BODY = ChildRole.LAST + 2;
    public const short DOCUMENT_END = ChildRole.LAST + 3;

    internal YamlDocument() : base() { }

    public override JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.NodeType NodeType =>
      JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl.ElementType.YAML_DOCUMENT;

    public override void Accept(JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.TreeNodeVisitor visitor) =>
      visitor.VisitYamlDocumentNode(this);

    public override void Accept<TContext>(JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.TreeNodeVisitor<TContext> visitor, TContext context) =>
      visitor.VisitYamlDocumentNode(this, context);

    public override TReturn Accept<TContext, TReturn>(JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.TreeNodeVisitor<TContext, TReturn> visitor, TContext context) =>
      visitor.VisitYamlDocumentNode(this, context);

    public override short GetChildRole(JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.TreeElement child)
    {
      switch (child.NodeType.Index)
      {
        case JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl.ElementType.DIRECTIVES_NODE_TYPE_INDEX:
          return DIRECTIVES;
        case JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl.ElementType.DOCUMENT_BODY_NODE_TYPE_INDEX:
          return BODY;
        case JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing.YamlTokenType.DOCUMENT_END_NODE_TYPE_INDEX:
          return DOCUMENT_END;
      }
      return 0;
    }

    public virtual JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.IDocumentBody Body =>
      (JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.IDocumentBody) FindChildByRole(BODY);

    public virtual JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.IDirectives Directives =>
      (JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.IDirectives) FindChildByRole(DIRECTIVES);

    public virtual JetBrains.ReSharper.Psi.Tree.TreeNodeCollection<JetBrains.ReSharper.Psi.Tree.ITokenNode> DocumentEndMarker =>
      FindListOfChildrenByRole<JetBrains.ReSharper.Psi.Tree.ITokenNode>(DOCUMENT_END);

    public virtual JetBrains.ReSharper.Psi.Tree.TreeNodeEnumerable<JetBrains.ReSharper.Psi.Tree.ITokenNode> DocumentEndMarkerEnumerable =>
      AsChildrenEnumerable<JetBrains.ReSharper.Psi.Tree.ITokenNode>(DOCUMENT_END);

    public virtual JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.IDocumentBody SetBody(JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.IDocumentBody param)
    {
      using (JetBrains.ReSharper.Resources.Shell.WriteLockCookie.Create(this.IsPhysical()))
      {
        JetBrains.ReSharper.Psi.Tree.ITreeNode current = null, next = GetNextFilteredChild(current);
        JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.IDocumentBody result = null;

        next = GetNextFilteredChild(current);
        if (next != null && next.NodeType == JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl.ElementType.DIRECTIVES)
        {
          next = GetNextFilteredChild(current);
          if (next != null && next.NodeType == JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl.ElementType.DIRECTIVES)
          {
            current = next;
          }
          else
          {
            return result;
          }

        }

        next = GetNextFilteredChild(current);
        if (next == null)
        {
          if (param == null) return null;
          current = result = JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.ModificationUtil.AddChildAfter(this, current, param);
        }
        else if (next.NodeType == JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl.ElementType.DOCUMENT_BODY)
        {
          if (param != null)
          {
            current = result = JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.ModificationUtil.ReplaceChild(next, param);
          }
          else
          {
            JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.ModificationUtil.DeleteChild(next);
          }
        }
        else
        {
          if (param == null) return null;
          result = JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.ModificationUtil.AddChildBefore(next, param);
          current = next;
        }

        return result;
      }
    }

    public virtual JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.IDirectives SetDirectives(JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.IDirectives param)
    {
      using (JetBrains.ReSharper.Resources.Shell.WriteLockCookie.Create(this.IsPhysical()))
      {
        JetBrains.ReSharper.Psi.Tree.ITreeNode current = null, next = GetNextFilteredChild(current);
        JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.IDirectives result = null;

        next = GetNextFilteredChild(current);
        if (next == null)
        {
          if (param == null) return null;
          current = result = JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.ModificationUtil.AddChildAfter(this, current, param);
        }
        else if (next.NodeType == JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl.ElementType.DIRECTIVES)
        {
          if (param != null)
          {
            current = result = JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.ModificationUtil.ReplaceChild(next, param);
          }
          else
          {
            JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.ModificationUtil.DeleteChild(next);
          }
        }
        else
        {
          if (param == null) return null;
          result = JetBrains.ReSharper.Psi.ExtensionsAPI.Tree.ModificationUtil.AddChildBefore(next, param);
          current = next;
        }

        return result;
      }
    }

    public override string ToString() => "IYamlDocument";
  }
}
