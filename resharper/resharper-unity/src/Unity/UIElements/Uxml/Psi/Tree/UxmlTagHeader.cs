#nullable enable
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl.Tree.References;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Tree
{
    internal class UxmlTagHeader : XmlTagHeaderNode
    {
        public UxmlTagHeader(XmlCompositeNodeType type) : base(type) { }

        protected override ReferenceCollection CreateFirstClassReferences()
        {
            var nameToken = this.Name;
            var name = nameToken.XmlName;
            var symbolCache = GetPsiServices().Symbols;
            
            IQualifier? qualifier = null;
            var references = new LocalList<IReference>();

            if (!nameToken.XmlNamespace.IsNullOrEmpty())
            {
                var xmlNamespaceRef = new XamlNamespaceAliasReference(this, nameToken, nameToken.XmlNamespaceRange);
                references.Add(xmlNamespaceRef);
            }
            
            var startIndex = Name.XmlNameRange.StartOffset.Offset;
            var nextDotIndex = name.IndexOf('.');
            while (true)
            {
                var endIndex = nextDotIndex != -1 ? nextDotIndex : Name.XmlNameRange.EndOffset.Offset;
                var rangeWithin = TreeTextRange.FromLength(new TreeOffset(startIndex), endIndex - startIndex);
                var isFinalPart = nextDotIndex == -1;
                var reference = new UxmlTypeOrNamespaceReference(this, qualifier, nameToken, rangeWithin,
                    symbolCache, isFinalPart);
            
                references.Add(reference);
                if (nextDotIndex == -1)
                    break;
            
                startIndex = nextDotIndex + 1;
                nextDotIndex = name.IndexOf('.', startIndex);
                qualifier = reference;
            }

            return new ReferenceCollection(references.ReadOnlyList());
        }
    }
}