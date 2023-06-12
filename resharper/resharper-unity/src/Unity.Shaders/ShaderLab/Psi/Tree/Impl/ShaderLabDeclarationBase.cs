#nullable enable
using System;
using System.Xml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabDeclarationBase : ShaderLabCompositeElement, IDeclaration
    {
        private readonly CachedPsiValueWithOffsets<IDeclaredElement?> myCachedDeclaredElement = new();
        
        public IDeclaredElement? DeclaredElement => myCachedDeclaredElement.GetValue(this, static self => self.TryCreateDeclaredElement());

        public string DeclaredName => GetName() ?? SharedImplUtil.MISSING_DECLARATION_NAME;
        public abstract string? GetName();
        public abstract TreeTextRange GetNameRange();
        public virtual void SetName(string name) => throw new NotSupportedException();
        protected abstract IDeclaredElement? TryCreateDeclaredElement();

        public bool IsSynthetic() => false;
        // Can we abuse this to return e.g. tooltips?
        public virtual XmlNode? GetXMLDoc(bool inherit) => null;
    }
}