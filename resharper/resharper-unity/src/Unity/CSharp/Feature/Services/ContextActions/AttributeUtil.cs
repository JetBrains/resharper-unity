using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    public static class AttributeUtil
    {
        public static IAttribute? AddAttributeToSingleDeclaration(IAttributesOwnerDeclaration fieldDeclaration,
                                                                  IClrTypeName attributeTypeName,
                                                                  IPsiModule module,
                                                                  CSharpElementFactory elementFactory)
        {
            return AddAttributeToSingleDeclaration(fieldDeclaration, attributeTypeName,
                EmptyArray<AttributeValue>.Instance,
                EmptyArray<Pair<string, AttributeValue>>.Instance,
                module, elementFactory);
        }

        public static IAttribute? AddAttributeToSingleDeclaration(IAttributesOwnerDeclaration fieldDeclaration,
                                                                  IClrTypeName attributeTypeName,
                                                                  AttributeValue[] fixedArguments,
                                                                  Pair<string, AttributeValue>[]? namedArguments,
                                                                  IPsiModule module,
                                                                  CSharpElementFactory elementFactory,
                                                                  bool allowMultiple = false)
        {
            // TODO: Should we do this check here?
            var existingAttribute = GetAttribute(fieldDeclaration, attributeTypeName);
            if (existingAttribute != null && !allowMultiple) return null;

            var attribute = CreateAttribute(attributeTypeName, fixedArguments, namedArguments, module, elementFactory);

            // This will split a multiple declaration, if necessary
            return attribute != null ? fieldDeclaration.AddAttributeAfter(attribute, null) : null;
        }

        public static IAttribute? AddAttributeToEntireDeclaration(IMultipleFieldDeclaration fieldDeclaration,
                                                                  IClrTypeName attributeTypeName,
                                                                  IPsiModule module,
                                                                  CSharpElementFactory elementFactory)
        {
            return AddAttributeToEntireDeclaration(fieldDeclaration, attributeTypeName,
                EmptyArray<AttributeValue>.Instance,
                EmptyArray<Pair<string, AttributeValue>>.Instance,
                module, elementFactory);
        }

        // Given a multiple field declaration (a declaration with multiple fields declared at once), adds an attribute
        // to the entire declaration, which when compiled has the effect of applying the attribute to each field
        public static IAttribute? AddAttributeToEntireDeclaration(IMultipleFieldDeclaration multipleFieldDeclaration,
                                                                  IClrTypeName attributeTypeName,
                                                                  AttributeValue[] fixedArguments,
                                                                  Pair<string, AttributeValue>[]? namedArguments,
                                                                  IPsiModule module,
                                                                  CSharpElementFactory elementFactory)
        {
            // TODO: Do we need to do this check here?
            var existingAttribute = GetAttribute(multipleFieldDeclaration.Attributes, attributeTypeName);
            if (existingAttribute != null) return null;

            var attribute = CreateAttribute(attributeTypeName, fixedArguments, namedArguments, module, elementFactory);
            if (attribute != null)
            {
                // It doesn't matter which declaration we use, it will be applied to the multiple field declaration
                var firstFieldDeclaration = (IFieldDeclaration)multipleFieldDeclaration.Declarators[0];
                return CSharpSharedImplUtil.AddAttributeAfter(firstFieldDeclaration, attribute, null);
            }

            return null;
        }

        private static IAttribute? CreateAttribute(IClrTypeName attributeTypeName,
                                                   AttributeValue[] fixedArguments,
                                                   Pair<string, AttributeValue>[]? namedArguments,
                                                   IPsiModule module,
                                                   CSharpElementFactory elementFactory)
        {
            var typeElement = KnownTypesFactory.GetByClrTypeName(attributeTypeName, module).GetTypeElement();
            namedArguments ??= EmptyArray<Pair<string, AttributeValue>>.Instance;
            return typeElement != null
                ? elementFactory.CreateAttribute(typeElement, fixedArguments, namedArguments)
                : null;
        }

        public static void RemoveAttributeFromSingleDeclaration(IFieldDeclaration fieldDeclaration,
            IClrTypeName attributeTypeName)
        {
            var attribute = GetAttribute(fieldDeclaration, attributeTypeName);
            if (attribute != null)
            {
                // This will split a multiple declaration, if necessary
                fieldDeclaration.RemoveAttribute(attribute);
            }
        }

        public static void RemoveAttributeFromAllDeclarations(IFieldDeclaration fieldDeclaration,
            IClrTypeName attributeTypeName)
        {
            var attribute = GetAttribute(fieldDeclaration, attributeTypeName);
            if (attribute != null)
                CSharpSharedImplUtil.RemoveAttribute(fieldDeclaration, attribute);
        }

        [ContractAnnotation("attributeSectionList:null => null")]
        private static IAttribute? GetAttribute(IAttributeSectionList? attributeSectionList,
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

        [ContractAnnotation("attributesOwner:null => null")]
        public static IAttribute? GetAttribute(this IAttributesOwnerDeclaration? attributesOwner,
                                               IClrTypeName requiredAttributeTypeName)
        {
            return GetAttributes(attributesOwner, requiredAttributeTypeName).FirstOrDefault(null);
        }

        public static IEnumerable<IAttribute> GetAttributes(IAttributesOwnerDeclaration? attributesOwner,
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

        public static Action<ITextControl> CreateHotspotSession(this IAttribute attribute)
        {
            var hotspotsRegistry = new HotspotsRegistry(attribute.GetSolution().GetPsiServices());

            var arguments = attribute.Arguments;
            foreach (var argument in arguments)
            {
                if (argument.Value is ICSharpLiteralExpression literalExpression)
                {
                    var range = literalExpression.Literal.GetUnquotedDocumentRange().CreateRangeMarker();
                    hotspotsRegistry.Register(range,
                        new NameSuggestionsExpression(new[]
                        {
                            literalExpression.ConstantValue.GetPresentation(attribute.Language, DeclaredElementPresenterTextStyles.Empty).Text
                        }));
                }
            }

            var propertyAssignments = attribute.PropertyAssignments;
            foreach (var argument in propertyAssignments)
            {
                if (argument.Source is ICSharpLiteralExpression literalExpression)
                {
                    var range = literalExpression.Literal.GetUnquotedDocumentRange().CreateRangeMarker();
                    hotspotsRegistry.Register(range,
                        new NameSuggestionsExpression(new[]
                        {
                            literalExpression.ConstantValue.GetPresentation(attribute.Language, DeclaredElementPresenterTextStyles.Empty).Text
                        }));
                }
            }

            return BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, DocumentOffset.InvalidOffset);
        }
    }
}
