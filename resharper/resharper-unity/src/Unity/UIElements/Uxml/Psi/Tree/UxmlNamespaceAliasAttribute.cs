using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xaml.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Tree
{
    public class UxmlNamespaceAliasAttribute : NamespaceAliasAttribute
    {
        public UxmlNamespaceAliasAttribute(XmlCompositeNodeType type) : base(type)
        {
        }
        
        protected override ReferenceCollection CreateFirstClassReferences()
        {
            var ranges = ParseNamespaceRanges(this);
            if (ranges.Count <= 0) return ReferenceCollection.Empty;

            var token = (IXmlValueToken) Value;
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
        public static IList<TreeTextRange> ParseNamespaceRanges([NotNull] INamespaceAlias alias)
        {
            var ranges = new LocalList<TreeTextRange>();
            var unquotedValue = alias.UnquotedValue;

            // Unity namespace references: 'UnityEngine.UIElements'
            var namespaceName = unquotedValue;
            var startOffset = 1; // opening quote
            foreach (var part in namespaceName.Split('.'))
            {
                var range = TreeTextRange.FromLength(new TreeOffset(startOffset), part.Length);
                ranges.Add(range);
                startOffset += part.Length + 1;
            }

            return ranges.ResultingList();
        }
    }
}