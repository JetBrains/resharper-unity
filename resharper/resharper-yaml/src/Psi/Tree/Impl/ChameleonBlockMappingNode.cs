using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl
{
  internal class ChameleonBlockMappingNode : BlockMappingNode, IChameleonBlockMappingNode
  {
    private readonly object mySyncObject = new object();
    private bool myOpened;

    public bool IsOpened
    {
      get
      {
        lock (mySyncObject)
        {
          if (myOpened) return myOpened;
          myOpened = !(firstChild is ClosedChameleonElement);
          return myOpened;
        }
      }
    }

    public override ITreeNode FirstChild
    {
      get
      {
        lock (mySyncObject)
        {
          if (!IsOpened) OpenChameleon();
          return firstChild;
        }
      }
    }

    public override ITreeNode LastChild
    {
      get
      {
        lock (mySyncObject)
        {
          if (!IsOpened) OpenChameleon();
          return lastChild;
        }
      }
    }

    public override int GetTextLength()
    {
      lock (mySyncObject)
        return base.GetTextLength();
    }

    public override StringBuilder GetText(StringBuilder to)
    {
      lock (mySyncObject)
        return base.GetText(to);
    }

    public override IBuffer GetTextAsBuffer()
    {
      lock (mySyncObject)
        return base.GetTextAsBuffer();
    }

    protected override TreeElement DeepClone(TreeNodeCopyContext context)
    {
      lock (mySyncObject)
        return base.DeepClone(context);
    }

    public IChameleonNode ReSync(CachingLexer cachingLexer, TreeTextRange changedRange, int insertedTextLen)
    {
      throw new System.NotImplementedException();
    }

    private void OpenChameleon()
    {
      Assertion.Assert(!myOpened, "The condition (!myOpened) is false.");
      AssertSingleChild();

      IClosedChameleonBody closedChameleon = null;
      for (var child = firstChild; child != null; child = child.nextSibling)
      {
        closedChameleon = child as IClosedChameleonBody;
        if (closedChameleon != null)
          break;
      }

      Assertion.AssertNotNull(closedChameleon, "closedChameleon != null");

      var openChameleon = closedChameleon.Parse(parser =>
      {
        var yamlParser = (IYamlParser) parser;
        return yamlParser.ParseRootBlockNode();
      });

      // Need to do this outside assertion check because it will calculate cached offsets for children
      var newTextLength = openChameleon.GetTextLength();

      Assertion.Assert(closedChameleon.GetTextLength() == newTextLength,
        "Chameleon length differ after opening! {0} {1} {2}", closedChameleon.GetTextLength(), newTextLength,
        GetSourceFile()?.DisplayName ?? "unknown file");

      DeleteChildRange(firstChild, lastChild);
      OpenChameleonFrom(openChameleon);
    }

    [Conditional("JET_MODE_ASSERT")]
    private void AssertSingleChild()
    {
      if (!((firstChild == lastChild) && (firstChild is IClosedChameleonBody)))
        Assertion.Fail("One ChameleonElement child but found also {0}", lastChild.NodeType);
    }

    private void OpenChameleonFrom([NotNull] ITreeNode chameleon)
    {
      Assertion.Assert(firstChild == null && lastChild == null, "firstChild == null && lastChild == null");

      ITreeNode element;
      while ((element = chameleon.FirstChild) != null)
      {
        ((CompositeElement) chameleon).DeleteChildRange(element, element);
        AppendNewChild ((TreeElement)element);
      }

      myOpened = true;
    }

    public override string ToString() => "ChameleonBlockMappingNode";
  }
}