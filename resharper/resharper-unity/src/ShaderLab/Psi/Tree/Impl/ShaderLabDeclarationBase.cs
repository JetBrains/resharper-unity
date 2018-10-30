using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabDeclarationBase : ShaderLabCompositeElement, IDeclaration
    {
        public abstract IDeclaredElement DeclaredElement { get; }

        public abstract string DeclaredName { get; }
        public abstract void SetName([NotNull] string name);
        public abstract TreeTextRange GetNameRange();

        public bool IsSynthetic() => false;
        // Can we abuse this to return e.g. tooltips?
        public virtual XmlNode GetXMLDoc(bool inherit) => null;
    }
}