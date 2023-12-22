#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Utils;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Collections;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference
{
    internal record FieldAdapter(bool IsValid,
        bool IsUnityFieldType,
        bool IsObjectTypeField,
        ElementId? CalculateElementId,
        string GetTypeFullName,
        bool IsTypeParameterType, string FieldName)
    {
        public static FieldAdapter InValidFieldAdapter =>
            new(false, false, false, null, String.Empty, false, String.Empty);
    }

    internal record ClassInfoAdapter(ElementId? ElementId,
        CountingSet<ElementId> SuperClasses,
        string FullyQualifiedName,
        Dictionary<ElementId, TypeParameter> TypeParametersDictionary,
        FieldAdapter[] SerializedFields,
        List<TypeParameterResolve> TypeParameterResolves);

    internal static class SerializeReferenceTypesUtils
    {
        private static readonly ILogger ourLogger = Logger.GetLogger<UnitySerializedReferenceProvider>();

        private static CountingSet<ElementId> CreateCountingSet(IEnumerable<KeyValuePair<ElementId, int>> elementIds)
        {
            var result = new CountingSet<ElementId>();

            foreach (var (key, value) in elementIds)
            {
                if (value <= 0)
                    throw new ArgumentException("Wrong value");

                result.Add(key, value);
            }

            return result;
        }

        private static IDeclaredType? GetUnityTypeOwnerType(ITypeOwner typeOwner)
        {
            Assertion.Require(typeOwner is IProperty or IField);

            var typeOwnerType = typeOwner.Type;

            if (typeOwnerType is IArrayType { Rank: 1 } arrayType) //array
                return arrayType.ElementType as IDeclaredType;

            if (typeOwnerType is not IDeclaredType declaredType)
                return null;


            var typeElement = declaredType.GetTypeElement();

            if (typeElement == null)
                return null;

            var typeParameters = typeElement.TypeParameters;

            if (typeParameters.Count == 0)
                return declaredType;

            var substitution = declaredType.GetSubstitution();

            if (declaredType.IsGenericList()) //List<>
                return substitution[typeParameters[0]] as IDeclaredType;

            return null;
        }

        private static Dictionary<ElementId, TypeParameter> GetTypeParametersDict(
            ITypeElement classLikeDeclaration,
            IUnityElementIdProvider unityElementIdProvider)
        {
            var result = new Dictionary<ElementId, TypeParameter>();

            foreach (var typeParameterOfTypeDeclaration in classLikeDeclaration.GetAllTypeParameters())
            {
                if (typeParameterOfTypeDeclaration == null)
                    continue;

                var elementId =
                    unityElementIdProvider.GetElementId(typeParameterOfTypeDeclaration, classLikeDeclaration);
                if (elementId == null)
                    continue;

                var declaredElementShortName = GetDeclaredElementNameDescription(classLikeDeclaration, typeParameterOfTypeDeclaration);

                result.Add(elementId.Value,
                    new TypeParameter(elementId.Value, declaredElementShortName, typeParameterOfTypeDeclaration.Index,
                        new CountingSet<ElementId>()));
            }

            return result;
        }

        private static Dictionary<ElementId, TypeParameter> GetTypeParametersDict(IMetadataTypeInfo metadataTypeInfo,
            IPsiAssemblyFile assemblyFile, IUnityElementIdProvider unityElementIdProvider)
        {
            var metadataTypeParameters = metadataTypeInfo.TypeParameters;
            var result = new Dictionary<ElementId, TypeParameter>();
            foreach (var typeParameter in metadataTypeParameters)
            {
                var elementId = unityElementIdProvider.GetElementId(typeParameter, assemblyFile);
                if (elementId == null)
                    continue;

                var parameterName = GetParameterNameDescription(typeParameter);

                var parameter = new TypeParameter(
                    elementId.Value,
                    parameterName,
                    (int)typeParameter.Index,
                    new CountingSet<ElementId>()
                );
                result.Add(elementId.Value, parameter);
            }

            return result;
        }

        internal static void CollectClassData(ClassInfoAdapter classInfoAdapter,
            ClassMetaInfoDictionary resultInfoTypeToInterfaces,
            CountingSet<TypeParameterResolve> resultTypeParameterResolves)
        {
            var classId = ProcessClassInfo(classInfoAdapter, resultInfoTypeToInterfaces);
            if (classId == null)
                return;

            foreach (var typeParameterResolve in classInfoAdapter.TypeParameterResolves)
                resultTypeParameterResolves.Add(typeParameterResolve);

            ProcessFields(classInfoAdapter, resultInfoTypeToInterfaces);
        }

        private static ElementId? ProcessClassInfo(ClassInfoAdapter classAdapter,
            ClassMetaInfoDictionary classMetaInfoDictionary)
        {
            var elementId = classAdapter.ElementId;
            if (elementId == null)
                return elementId;

            var classId = elementId.Value;

            var typeParameters = classAdapter.TypeParametersDictionary;


            var classMetaInfo = new ClassMetaInfo(
                classAdapter.FullyQualifiedName,
                classAdapter.SuperClasses,
                new CountingSet<ElementId>(),
                typeParameters
            );

            if (classMetaInfoDictionary.TryGetValue(classId, out var existedValue))
            {
                ourLogger.Verbose(
                    $"ClassId {classId} already exists. Existed class:{existedValue}, new info: {classMetaInfo}");
                existedValue.UnionWith(classMetaInfo);
            }
            else
            {
                ourLogger.Trace(
                    $"Adding id:{classId}, name:{classMetaInfo.ClassName}, {classAdapter.FullyQualifiedName}");
                classMetaInfoDictionary.Add(classId, classMetaInfo);
            }

            return classId;
        }

        private static void ProcessFields(ClassInfoAdapter classInfoAdapter,
            ClassMetaInfoDictionary resultInfoTypeToInterfaces)
        {
            foreach (var fieldAdapter in classInfoAdapter.SerializedFields)
                CollectFieldData(fieldAdapter,
                    classInfoAdapter.ElementId!.Value,
                    resultInfoTypeToInterfaces);
        }

        private static void CollectFieldData(FieldAdapter field, ElementId classId
            , ClassMetaInfoDictionary resultInfoTypeToInterfaces)
        {
            if (!field.IsValid) return;

            if (!field.IsUnityFieldType) return;

            //object fields won't be serialized in Unity
            if (field.IsObjectTypeField) return;

            var fieldTypeId = field.CalculateElementId;
            if (fieldTypeId == null)
                return;

            if (field.IsTypeParameterType)
            {
                if (resultInfoTypeToInterfaces.TryGetValue(classId, out var containingTypeMetaInfo))
                {
                    if (containingTypeMetaInfo.TypeParameters.TryGetValue(fieldTypeId.Value, out var typeParameter))
                        typeParameter.SerializeReferenceHolders.Add(classId);
                    else //- class is already processed with all type parameters
                        Assertion.Fail(
                            $"TypeParameter should already exists, {nameof(field.FieldName)}:{field.FieldName}");
                }
                else
                {
                    Assertion.Fail($"ClassId '{classId.Value}' should already exists in index");
                }
            }
            else if (resultInfoTypeToInterfaces.TryGetValue(fieldTypeId.Value, out var originalClassMetaInfo))
            {
                originalClassMetaInfo.SerializeReferenceHolders.Add(classId);
            }
            else
            {
                var fieldClassMetaInfo = new ClassMetaInfo(field.GetTypeFullName);
                fieldClassMetaInfo.SerializeReferenceHolders.Add(classId);
                resultInfoTypeToInterfaces.Add(fieldTypeId.Value, fieldClassMetaInfo);
            }
        }

        private static FieldAdapter ToAdapter(this IMetadataField? metadataField, IPsiAssemblyFile assemblyFile,
            IUnityElementIdProvider unityElementIdProvider)
        {
            //Property with backing field will be represented as IMetadataField
            var isValid = metadataField is { IsStatic: false, IsLiteral: false, IsInitOnly: false, NotSerialized: false };
            if (!isValid)
                return FieldAdapter.InValidFieldAdapter;

            var metadataFieldType = GetUnityFieldType(metadataField);

            var isUnityFieldType = metadataFieldType != null;
            var isObjectTypeField = metadataFieldType != null &&
                                    Equals(metadataFieldType.FullName, PredefinedType.OBJECT_FQN.FullName);
            var elementId = metadataFieldType != null
                ? unityElementIdProvider.GetElementId(metadataFieldType, assemblyFile)
                : null;
            var typeFullName = GetTypeFullNameDescription(metadataFieldType);
            var fieldName = GetMetadataFieldNameDescription(metadataField);
            var isTypeParameterType = metadataFieldType is IMetadataTypeParameterReferenceType;

            return new FieldAdapter(isValid, isUnityFieldType, isObjectTypeField, elementId, typeFullName,
                isTypeParameterType, fieldName);
        }

        private static IMetadataType? GetUnityFieldType(IMetadataField? metadataField)
        {
            if (metadataField == null)
                return null;

            var fieldType = metadataField.Type;

            switch (fieldType)
            {
                case IMetadataTypeParameterReferenceType:
                    return fieldType;
                case IMetadataArrayType { Rank: 1 } metadataArrayType:
                    return metadataArrayType.ElementType;
                case IMetadataClassType metadataClassType:
                {
                    var typeParameters = metadataClassType.Arguments;

                    if (typeParameters.Length == 0)
                        return metadataClassType;

                    if (metadataClassType.Type.IsList()) //List<>
                        return typeParameters[0];

                    break;
                }
            }

            return null;
        }

        public static bool HasFieldAttribute(this IProperty property, IClrTypeName clrTypeName)
        {
            Assertion.Require(property != null, "property != null");
            
            var attributes = property.GetDeclarations<IPropertyDeclaration>()
                .SelectMany(declaration => declaration.AttributesEnumerable);
            foreach (var attribute in attributes)
            {
                if (attribute is not { Target: AttributeTarget.Field }) 
                    continue;
                
                var result = attribute.Name.Reference.Resolve();
                    
                if (result.ResolveErrorType != ResolveErrorType.OK || result.DeclaredElement is not ITypeElement typeElement) 
                    continue;
                    
                if (Equals(typeElement.GetClrName(), clrTypeName))
                    return true;
            }

            return false;
        }

        private static FieldAdapter ToAdapter(this ITypeOwner? typeOwner, ITypeElement ownerTypeElement,
            IUnityElementIdProvider provider)
        {
            Assertion.Require(typeOwner is IField or IProperty);

            var isValid = typeOwner is IProperty { IsStatic: false, IsReadonly: false, IsAuto: true, IsWritable: true } property
                          && !property.HasFieldAttribute(PredefinedType.NONSERIALIZED_ATTRIBUTE_CLASS)
                          ||
                          typeOwner is IField { IsStatic: false, IsConstant: false, IsReadonly: false } field
                          && !field.HasAttributeInstance(PredefinedType.NONSERIALIZED_ATTRIBUTE_CLASS, false);

            if (!isValid)
                return FieldAdapter.InValidFieldAdapter;

            var declaredType = GetUnityTypeOwnerType(typeOwner);
            var isUnityFieldType = declaredType != null;
            var isObjectTypeField =
                declaredType != null && Equals(declaredType.GetClrName(), PredefinedType.OBJECT_FQN);
            var elementId = declaredType != null
                ? provider.GetElementId(declaredType.GetTypeElement(), ownerTypeElement)
                : null;
            var typeFullName = GetTypeFullNameDescription(declaredType);
            var fieldName = GetFieldNameDescription(typeOwner);
            var isTypeParameterType = declaredType?.IsOpenType ?? false;

            return new FieldAdapter(isValid, isUnityFieldType, isObjectTypeField, elementId, typeFullName,
                isTypeParameterType, fieldName);
        }


        internal static ClassInfoAdapter ToAdapter(this IMetadataTypeInfo classType, IPsiAssemblyFile assemblyFile,
            IUnityElementIdProvider provider)
        {
            var elementId = provider.GetElementId(classType, assemblyFile);

            var metadataSuperClassTypes =
                classType.InterfaceImplementations.Select(i => i.Interface)
                    .Concat(new[] { classType.Base }).Where(t => t != null).ToList();

            var superClasses =
                metadataSuperClassTypes
                    .Where(t => t != null && !t.IsObject() && !t.Type.IsArray() && !t.Type.IsList())
                    .Select(t => provider.GetElementId(t!.Type, assemblyFile))
                    .Select(id => new KeyValuePair<ElementId, int>(id!.Value, 1));

            var fullyQualifiedName = GetClassTypeFullyQualifiedNameDescription(classType);
            var typeParametersDictionary =
                GetTypeParametersDict(classType, assemblyFile, provider);

            var serializedFields = classType.GetFields()
                .Where(f => f.HasCustomAttribute(KnownTypes.SerializeReference.FullName))
                .Select(f => f.ToAdapter(assemblyFile, provider)).ToArray();

            var typeResolves = GetTypeParameterResolves(assemblyFile, provider, metadataSuperClassTypes);

            return new ClassInfoAdapter(elementId, CreateCountingSet(superClasses), fullyQualifiedName,
                typeParametersDictionary, serializedFields, typeResolves);
        }

        private static List<TypeParameterResolve> GetTypeParameterResolves(IPsiAssemblyFile assemblyFile,
            IUnityElementIdProvider provider,
            IEnumerable<IMetadataClassType> metadataSuperClassTypes)
        {
            var typeResolves = new List<TypeParameterResolve>();

            foreach (var superClassType in metadataSuperClassTypes)
            {
                if (superClassType == null || Equals(superClassType.FullName, PredefinedType.OBJECT_FQN.FullName))
                    continue;

                for (var index = 0; index < superClassType.Arguments.Length; index++)
                {
                    var metadataType = superClassType.Arguments[index];

                    var parameterId =
                        provider.GetElementId(superClassType.Type.TypeParameters[index], assemblyFile);

                    if (parameterId == null)
                        continue;

                    ElementId? resolvedId = null;
                    var typeParameterName = string.Empty;

                    switch (metadataType)
                    {
                        case IMetadataTypeParameterReferenceType parameterReferenceType:
                            resolvedId = provider.GetElementId(parameterReferenceType.TypeParameter, assemblyFile);
                            typeParameterName = GetTypeParameterNameDescription(parameterReferenceType);
                            break;
                        case IMetadataClassType parameterClassType:
                            resolvedId = provider.GetElementId(parameterClassType.Type, assemblyFile);
                            typeParameterName = GetTypeParameterNameDescription(parameterClassType);
                            break;
                    }

                    if (resolvedId == null)
                        continue;


                    typeResolves.Add(new TypeParameterResolve(
                        GetResolutionDescription(superClassType, index, metadataType, typeParameterName),
                        parameterId.Value,
                        resolvedId.Value
                    ));
                }
            }

            return typeResolves;
        }


        internal static ClassInfoAdapter ToAdapter(this ITypeElement typeElement,
            IUnityElementIdProvider provider)
        {
            Assertion.Require(typeElement != null, "typeElement != null");
            Assertion.Require(provider != null, "provider != null");

            var elementId = provider.GetElementId(typeElement);
            var superTypes = typeElement.GetSuperTypes().Where(t => !t.IsObject()).ToList();

            var superClassesEnumerable = superTypes
                .Select(i => i.GetTypeElement())
                .Where(i => i != null
                            && !i.IsObjectClass()
                            && i.Type() is not IArrayType { Rank: 1 }
                            && !i.Type().IsGenericList())
                .Select(i => provider.GetElementId(i))
                .Where(id => id != null)
                .Select(id => new KeyValuePair<ElementId, int>(id!.Value, 1));

            var fullyQualifiedName = GetFullyQualifiedNameDescription(typeElement);

            var typeParametersDictionary = GetTypeParametersDict(typeElement, provider);

            var serializedRefFields = typeElement.Fields
                .Where(field => field != null && field.HasAttributeInstance(KnownTypes.SerializeReference, false))
                .Select(f => f.ToAdapter(typeElement, provider));

            var serializedRefProperties = typeElement.Properties
                .Where(property => property != null)
                .Where(property => property.HasFieldAttribute(KnownTypes.SerializeReference))
                .Select(f => f.ToAdapter(typeElement, provider));

            var typeResolves = GetTypeParameterResolves(provider, superTypes);


            return new ClassInfoAdapter(elementId, CreateCountingSet(superClassesEnumerable), fullyQualifiedName,
                typeParametersDictionary, serializedRefFields.Concat(serializedRefProperties).ToArray(), typeResolves);
        }

        private static List<TypeParameterResolve> GetTypeParameterResolves(IUnityElementIdProvider provider,
            List<IDeclaredType> superTypes)
        {
            var typeResolves = new List<TypeParameterResolve>();

            foreach (var superClass in superTypes)
            {
                var resolveResult = superClass.Resolve();

                var superClassTypeElement = superClass.GetTypeElement();
                if (superClassTypeElement == null)
                    continue;

                var resolveResultSubstitution = resolveResult.Substitution;
                var typeParameters = resolveResultSubstitution.Domain;

                for (var index = 0; index < typeParameters.Count; index++)
                {
                    var typeParameter = typeParameters[index];
                    var type = resolveResultSubstitution[typeParameter];

                    var typeParamElementId = provider.GetElementId(typeParameter, superClassTypeElement, index);

                    if (typeParamElementId == null)
                        continue;

                    var declaredElement = type.GetTypeElement();
                    if (declaredElement == null)
                        continue;

                    var resolvedTypeElementId = provider.GetElementId(declaredElement);
                    if (resolvedTypeElementId == null)
                        continue;

                    typeResolves.Add(new TypeParameterResolve(
                        GetResolutionDescription(typeParameter, declaredElement),
                        typeParamElementId.Value, resolvedTypeElementId.Value
                    ));
                }
            }

            return typeResolves;
        }

        #region Additiona debug info

        private static bool IsDetailedInfoEnabled()
        {
            return ourLogger.IsEnabled(LoggingLevel.TRACE);
        }

        private static string GetResolutionDescription(ITypeParameter typeParameter, ITypeElement declaredElement)
        {
            if (IsDetailedInfoEnabled())
                return $"{typeParameter.OwnerType?.ShortName}:[{typeParameter.Index}]{typeParameter.ShortName}->{declaredElement.ShortName}";
            return string.Empty;
        }

        private static string GetFullyQualifiedNameDescription(ITypeElement typeElement)
        {
            if (IsDetailedInfoEnabled())
                return typeElement.GetClrName().FullName;
            return string.Empty;
        }

        private static string GetResolutionDescription(IMetadataClassType superClassType, int index,
            IMetadataType metadataType, string typeParameterName)
        {
            if (IsDetailedInfoEnabled())
                return $"{superClassType.FullName}:[{index}]{metadataType.FullName}->{typeParameterName}";
            return string.Empty;
        }

        private static string GetTypeParameterNameDescription(IMetadataClassType parameterClassType)
        {
            if (IsDetailedInfoEnabled())
                return parameterClassType.FullName;
            return string.Empty;
        }

        private static string GetTypeParameterNameDescription(
            IMetadataTypeParameterReferenceType parameterReferenceType)
        {
            if (IsDetailedInfoEnabled())
                return parameterReferenceType.TypeParameter.Name;
            return string.Empty;
        }

        private static string GetDeclaredElementNameDescription(ITypeElement classLikeDeclaration,
            ITypeParameter typeParameterOfTypeDeclaration)
        {
            if (IsDetailedInfoEnabled())
                return $"{classLikeDeclaration.GetClrName()}<{typeParameterOfTypeDeclaration.ShortName}[{typeParameterOfTypeDeclaration.Index}]>";
            return string.Empty;
        }

        private static string GetParameterNameDescription(IMetadataTypeParameter typeParameter)
        {
            if (IsDetailedInfoEnabled())
                return $"{typeParameter.TypeOwner.FullyQualifiedName}<{typeParameter.Name}[{typeParameter.Index}]>";
            return string.Empty;
        }

        private static string GetMetadataFieldNameDescription(IMetadataField? metadataField)
        {
            if (IsDetailedInfoEnabled())
                return metadataField?.Name ?? string.Empty;
            return string.Empty;
        }

        private static string GetTypeFullNameDescription(IMetadataType? metadataFieldType)
        {
            if (IsDetailedInfoEnabled())
                return metadataFieldType != null ? metadataFieldType.FullName : string.Empty;
            return string.Empty;
        }

        private static string GetFieldNameDescription(ITypeOwner? typeOwner)
        {
            if (IsDetailedInfoEnabled())
                return typeOwner?.ShortName ?? string.Empty;
            return string.Empty;
        }

        private static string GetTypeFullNameDescription(IDeclaredType? declaredType)
        {
            if (IsDetailedInfoEnabled())
                return declaredType != null ? declaredType.GetClrName().FullName : string.Empty;
            return string.Empty;
        }

        private static string GetClassTypeFullyQualifiedNameDescription(IMetadataTypeInfo classType)
        {
            if (IsDetailedInfoEnabled())
                return classType.FullyQualifiedName;
            return string.Empty;
        }

        #endregion
    }
}
