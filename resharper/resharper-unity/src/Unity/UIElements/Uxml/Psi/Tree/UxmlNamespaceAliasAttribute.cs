using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Impl.Util;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using XmlAttribute = JetBrains.ReSharper.Psi.Xml.Impl.Tree.XmlAttribute;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Tree
{
    public partial class UxmlNamespaceAliasAttribute : XmlAttribute, IDeclaration
    {
        public UxmlNamespaceAliasAttribute(XmlCompositeNodeType type) : base(type)
        {
        }

        protected override ReferenceCollection CreateFirstClassReferences()
        {
            var ranges = ParseNamespaceRanges(this);
            if (ranges.Count <= 0) return ReferenceCollection.Empty;

            var token = (IXmlValueToken)Value;
            var references = new List<IReference>();

            IUxmlNamespaceReference qualifier = null;
            foreach (var range in ranges)
            {
                IUxmlNamespaceReference reference;
                if (qualifier == null)
                    reference = new UxmlRootNamespaceReference(this, token, range);
                else
                    reference = new UxmlNamespaceReference(this, qualifier, token, range);

                references.Add(reference);
                qualifier = reference;
            }

            return new ReferenceCollection(references);
        }

        [NotNull]
        public static IList<TreeTextRange> ParseNamespaceRanges([NotNull] XmlAttribute alias)
        {
            var ranges = new LocalList<TreeTextRange>();
            var unquotedValue = alias.UnquotedValue;

            // Unity namespace references: 'UnityEngine.UIElements'
            var namespaceName = unquotedValue;
            var startOffset = 1; // opening quote
            foreach (var part in namespaceName.Split('.'))
            {
                ranges.Add(TreeTextRange.FromLength(new TreeOffset(startOffset), part.Length));
                startOffset += part.Length + 1;
            }

            return ranges.ResultingList();
        }

        public IDeclaredElement DeclaredElement => this;

        public string DeclaredName => GetNameRange().IsValid() ? XmlName : string.Empty;

        public TreeTextRange GetNameRange()
        {
            if (XmlNamespaceRange.IsValid())
            {
                var nameRange = XmlNameRange;
                Assertion.Assert(nameRange.IsValid());
                return nameRange.Shift(GetTreeStartOffset());
            }

            return TreeTextRange.InvalidRange;
        }

        public bool IsSynthetic() => false;

        public void SetName(string name)
        {
            var range = GetNameRange(); 
            Assertion.Assert(range.IsValid());

            range = range.Shift(-Identifier.GetTreeStartOffset().Offset);
            if (name == string.Empty)
                range = new TreeTextRange(new TreeOffset("xmlns".Length), range.EndOffset);

            using (WriteLockCookie.Create(IsPhysical()))
            {
                ReferenceWithTokenUtil.SetText(Identifier, range, name, this);
            }
        }

        public XmlNode GetXMLDoc(bool inherit) { return null; }
    }
}