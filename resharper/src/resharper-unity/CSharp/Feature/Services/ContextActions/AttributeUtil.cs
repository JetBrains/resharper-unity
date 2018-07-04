using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    public static class AttributeUtil
    {
        public static void AddAttributeToSingleDeclaration([CanBeNull] IFieldDeclaration fieldDeclaration,
            IClrTypeName attributeTypeName, IPsiModule psiModule, CSharpElementFactory elementFactory)
        {
            if (fieldDeclaration == null) return;

            var existingAttribute = GetAttribute(fieldDeclaration, attributeTypeName);
            if (existingAttribute != null)
                return;

            var attribute = CreateAttribute(attributeTypeName, psiModule, elementFactory);
            if (attribute != null)
                fieldDeclaration.AddAttributeAfter(attribute, null);
        }

        public static void AddAttributeToAllDeclarations([CanBeNull] IFieldDeclaration fieldDeclaration,
            IClrTypeName attributeTypeName, IPsiModule psiModule, CSharpElementFactory elementFactory)
        {
            if (fieldDeclaration == null) return;

            var existingAttribute = GetAttribute(fieldDeclaration, attributeTypeName);
            if (existingAttribute != null)
                return;

            var attribute = CreateAttribute(attributeTypeName, psiModule, elementFactory);
            if (attribute != null)
                CSharpSharedImplUtil.AddAttributeAfter(fieldDeclaration, attribute, null);
        }

        [CanBeNull]
        private static IAttribute CreateAttribute(IClrTypeName attributeTypeName, IPsiModule module,
            CSharpElementFactory elementFactory)
        {
            var typeElement = TypeFactory.CreateTypeByCLRName(attributeTypeName, module).GetTypeElement();
            return typeElement != null ? elementFactory.CreateAttribute(typeElement) : null;
        }

        public static void RemoveAttributeFromSingleDeclaration([CanBeNull] IFieldDeclaration fieldDeclaration,
            IClrTypeName attributeTypeName)
        {
            var attribute = GetAttribute(fieldDeclaration, attributeTypeName);
            if (attribute != null)
                fieldDeclaration.RemoveAttribute(attribute);
        }

        public static void RemoveAttributeFromAllDeclarations([CanBeNull] IFieldDeclaration fieldDeclaration,
            IClrTypeName attributeTypeName)
        {
            var attribute = GetAttribute(fieldDeclaration, attributeTypeName);
            if (attribute != null)
                CSharpSharedImplUtil.RemoveAttribute(fieldDeclaration, attribute);
        }

        [CanBeNull, ContractAnnotation("attributesOwner:null => null")]
        public static IAttribute GetAttribute([CanBeNull] IAttributesOwnerDeclaration attributesOwner,
            IClrTypeName requiredAttributeTypeName)
        {
            if (attributesOwner == null) return null;

            foreach (var attribute in attributesOwner.AttributesEnumerable)
            {
                if (attribute.TypeReference?.Resolve().DeclaredElement is ITypeElement typeElement)
                {
                    var attributeTypeName = typeElement.GetClrName();
                    if (Equals(attributeTypeName, requiredAttributeTypeName))
                        return attribute;
                }
            }

            return null;
        }
    }
}