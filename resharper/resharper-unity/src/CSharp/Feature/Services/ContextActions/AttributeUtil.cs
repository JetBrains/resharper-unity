using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    public static class AttributeUtil
    {
        [CanBeNull]
        public static IAttribute AddAttributeToSingleDeclaration([CanBeNull] IAttributesOwnerDeclaration fieldDeclaration,
                                                                 IClrTypeName attributeTypeName,
                                                                 [NotNull] AttributeValue[] attributeValues,
                                                                 IPsiModule module,
                                                                 CSharpElementFactory elementFactory)
        {
            if (fieldDeclaration == null) return null;

            // TODO: Should we do this check here?
            var existingAttribute = GetAttribute(fieldDeclaration, attributeTypeName);
            if (existingAttribute != null) return null;

            var attribute = CreateAttribute(attributeTypeName, attributeValues, module, elementFactory);
            if (attribute != null)
            {
                // This will split a multiple declaration, if necessary
                return fieldDeclaration.AddAttributeAfter(attribute, null);
            }

            return null;
        }

        [CanBeNull]
        public static IAttribute AddAttributeToSingleDeclaration([CanBeNull] IAttributesOwnerDeclaration fieldDeclaration,
            IClrTypeName attributeTypeName, IPsiModule module, CSharpElementFactory elementFactory)
        {
            return AddAttributeToSingleDeclaration(fieldDeclaration, attributeTypeName,
                EmptyArray<AttributeValue>.Instance, module, elementFactory);
        }

        // Given a multiple field declaration (a declaration with multiple fields declared at once), adds an attribute
        // to the entire declaration, which when compiled has the effect of applying the attribute to each field
        [CanBeNull]
        public static IAttribute AddAttributeToEntireDeclaration(
            [NotNull] IMultipleFieldDeclaration multipleFieldDeclaration,
            IClrTypeName attributeTypeName,
            [NotNull] AttributeValue[] attributeValues,
            IPsiModule module,
            CSharpElementFactory elementFactory)
        {
            // TODO: Do we need to do this check here?
            var existingAttribute = GetAttribute(multipleFieldDeclaration.Attributes, attributeTypeName);
            if (existingAttribute != null) return null;

            var attribute = CreateAttribute(attributeTypeName, attributeValues, module, elementFactory);
            if (attribute != null)
            {
                // It doesn't matter which declaration we use, it will be applied to the multiple field declaration
                var firstFieldDeclaration = (IFieldDeclaration) multipleFieldDeclaration.Declarators[0];
                return CSharpSharedImplUtil.AddAttributeAfter(firstFieldDeclaration, attribute, null);
            }

            return null;
        }

        private static IAttribute CreateAttribute(IClrTypeName attributeTypeName,
                                                  [NotNull] AttributeValue[] attributeValues,
                                                  IPsiModule module,
                                                  CSharpElementFactory elementFactory)
        {
            var typeElement = TypeFactory.CreateTypeByCLRName(attributeTypeName, module).GetTypeElement();
            return typeElement != null
                ? elementFactory.CreateAttribute(typeElement, attributeValues,
                    EmptyArray<Pair<string, AttributeValue>>.Instance)
                : null;
        }

        public static void RemoveAttributeFromSingleDeclaration([CanBeNull] IFieldDeclaration fieldDeclaration,
            IClrTypeName attributeTypeName)
        {
            var attribute = GetAttribute(fieldDeclaration, attributeTypeName);
            if (attribute != null)
            {
                // This will split a multiple declaration, if necessary
                fieldDeclaration.RemoveAttribute(attribute);
            }
        }

        public static void RemoveAttributeFromAllDeclarations([CanBeNull] IFieldDeclaration fieldDeclaration,
            IClrTypeName attributeTypeName)
        {
            var attribute = GetAttribute(fieldDeclaration, attributeTypeName);
            if (attribute != null)
                CSharpSharedImplUtil.RemoveAttribute(fieldDeclaration, attribute);
        }

        [CanBeNull, ContractAnnotation("attributeSectionList:null => null")]
        private static IAttribute GetAttribute([CanBeNull] IAttributeSectionList attributeSectionList,
                                               IClrTypeName requiredAttributeTypeName)
        {
            if (attributeSectionList == null) return null;

            foreach (var attribute in attributeSectionList.Attributes)
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

        [CanBeNull, ContractAnnotation("attributesOwner:null => null")]
        public static IAttribute GetAttribute([CanBeNull] IAttributesOwnerDeclaration attributesOwner,
            IClrTypeName requiredAttributeTypeName)
        {
            return GetAttributes(attributesOwner, requiredAttributeTypeName).FirstOrDefault(null);
        }
        
        public static IEnumerable<IAttribute> GetAttributes([CanBeNull] IAttributesOwnerDeclaration attributesOwner,
            IClrTypeName requiredAttributeTypeName)
        {
            if (attributesOwner == null) yield break;

            foreach (var attribute in attributesOwner.AttributesEnumerable)
            {
                if (attribute.TypeReference?.Resolve().DeclaredElement is ITypeElement typeElement)
                {
                    var attributeTypeName = typeElement.GetClrName();
                    if (Equals(attributeTypeName, requiredAttributeTypeName))
                        yield return attribute;
                }
            }
        }
    }
}