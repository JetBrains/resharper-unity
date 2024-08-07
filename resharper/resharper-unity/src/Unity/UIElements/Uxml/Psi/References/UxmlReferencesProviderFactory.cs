#nullable enable

using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References
{
    [ReferenceProviderFactory(ReferenceTypes = [typeof(UxmlNsAliasReference), typeof(UxmlTypeOrNamespaceReference)])]
    public class UxmlReferencesProviderFactory : IReferenceProviderFactory
    {
        public UxmlReferencesProviderFactory(Lifetime lifetime)
        {
            Changed = new DataFlow.Signal<IReferenceProviderFactory>(GetType().FullName!);
        }

        public IReferenceFactory? CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex? wordIndexForChecks)
        {
            if (!sourceFile.IsValid())
                return null;

            if (!sourceFile.GetProject().IsUnityProject())
                return null;

            if (sourceFile.IsUxml() && sourceFile.IsLanguageSupported<UxmlLanguage>())
                return new UxmlReferenceFactory();

            return null;
        }

        public DataFlow.ISignal<IReferenceProviderFactory> Changed { get; }
    }

    internal class UxmlReferenceFactory : IReferenceFactory
    {
        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            IXmlIdentifier? nameToken = null;
            IXmlTreeNode? xmlTreeNode = null;

            switch (element)
            {
                case XmlTagHeaderNode header:
                    nameToken = header.Name;
                    xmlTreeNode = header;
                    break;
                case XmlTagFooterNode footer:
                    nameToken = footer.Name;
                    xmlTreeNode = footer;
                    break;
            }

            if (nameToken == null || xmlTreeNode == null) return ReferenceCollection.Empty;

            if (nameToken.XmlNamespace == "ui" && nameToken.XmlName == "UXML") return ReferenceCollection.Empty;

            var symbolCache = element.GetPsiServices().Symbols;
            var references = new LocalList<IReference?>();
            var name = nameToken.XmlName;
            UxmlNsAliasReference? xmlNamespaceRef = null;
            if (!nameToken.XmlNamespace.IsNullOrEmpty())
            {
                xmlNamespaceRef = new UxmlNsAliasReference(xmlTreeNode, nameToken);
                references.Add(xmlNamespaceRef);
            }

            IQualifier? qualifier = xmlNamespaceRef;

            var startIndex = nameToken.XmlNameRange.StartOffset.Offset;
            var nextDotIndex = name.IndexOf('.');
            while (true)
            {
                var endIndex = nextDotIndex != -1 ? nextDotIndex : nameToken.XmlNameRange.EndOffset.Offset;
                var rangeWithin = TreeTextRange.FromLength(new TreeOffset(startIndex), endIndex - startIndex);
                var isFinalPart = nextDotIndex == -1;
                var reference = new UxmlTypeOrNamespaceReference(xmlTreeNode, qualifier, nameToken, rangeWithin,
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

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            return element is XmlTagHeaderNode header && names.HasAnyNameIn(header.Name.XmlName)
                   || element is XmlTagFooterNode footer && names.HasAnyNameIn(footer.Name.XmlName);
        }
    }
}