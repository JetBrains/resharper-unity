#nullable enable
using System.Xml;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class ShaderLabFile : IDeclaration
    {
        private readonly CachedPsiValueWithOffsets<IDeclaredElement?> myCachedDeclaredElement = new();
        
        private ITokenNode? ShaderNameToken => Command?.GetEntityNameToken();
        
        public IDeclaredElement? DeclaredElement => myCachedDeclaredElement.GetValue(this, static self => self.TryCreateDeclaredElement());
        public string DeclaredName => ShaderNameToken?.GetUnquotedText() is { Length: > 0 } name ? name : SharedImplUtil.MISSING_DECLARATION_NAME;
        public void SetName(string name) => this.SetStringLiteral(ShaderNameToken, name);

        public TreeTextRange GetNameRange() => ShaderNameToken?.GetUnquotedTreeTextRange() ?? TreeTextRange.InvalidRange;

        private IDeclaredElement? TryCreateDeclaredElement() => GetSourceFile() is { } sourceFile && ShaderNameToken is { } name ? new ShaderDeclaredElement(name.GetUnquotedText(), sourceFile, GetTreeStartOffset().Offset) : null;

        public bool IsSynthetic() => false;
        public XmlNode? GetXMLDoc(bool inherit) => null;
    }
}