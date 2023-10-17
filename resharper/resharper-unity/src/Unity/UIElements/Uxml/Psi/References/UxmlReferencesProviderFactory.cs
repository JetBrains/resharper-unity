#nullable enable

using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References
{
    [ReferenceProviderFactory]
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

            if (sourceFile.IsUxml() && sourceFile.IsLanguageSupported<UxmlLanguage>() )
                return new UxmlReferenceFactory();

            return null;
        }

        public DataFlow.ISignal<IReferenceProviderFactory> Changed { get; }
    }
    
    internal class UxmlReferenceFactory : IReferenceFactory
    {
        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            if (element is not (XmlTagHeaderNode xmlTagHeaderNode)) return ReferenceCollection.Empty;
            if (xmlTagHeaderNode.ContainerName == "ui:UXML") return ReferenceCollection.Empty;
            
            var nameToken = xmlTagHeaderNode.Name;
            var name = nameToken.XmlName;
            var symbolCache = xmlTagHeaderNode.GetPsiServices().Symbols;
            
            var references = new LocalList<IReference?>();
            UxmlNsAliasReference? xmlNamespaceRef = null;
            if (!nameToken.XmlNamespace.IsNullOrEmpty())
            {
                xmlNamespaceRef = new UxmlNsAliasReference(xmlTagHeaderNode, nameToken, nameToken.XmlNamespaceRange);
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
                var reference = new UxmlTypeOrNamespaceReference(xmlTagHeaderNode, qualifier, nameToken, rangeWithin,
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
            return element is XmlTagHeaderNode xmlIdentifier && names.HasAnyNameIn(xmlIdentifier.Name.XmlName);
        }
    }
}