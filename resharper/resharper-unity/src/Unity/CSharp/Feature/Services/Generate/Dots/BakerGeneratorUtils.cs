#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots
{
    public static class ComponentToAuthoringConverter
    {
        private static readonly Dictionary<IClrTypeName, ConversionData> ourComponentDataToAuthoringTypesConversion = new() 
        {
            {KnownTypes.Entity, new ConversionData(KnownTypes.GameObject, "GetEntity($0.$1, TransformUsageFlags.Dynamic)")},
            {KnownTypes.Random, new ConversionData(PredefinedType.UINT_FQN, "Unity.Mathematics.Random.CreateFromIndex($0.$1)")}
        };

        public static ConversionData? Convert(IClrTypeName clrTypeName, IPsiModule psiModule)
        {
            if (ourComponentDataToAuthoringTypesConversion.TryGetValue(clrTypeName, out var result))
                return result;
           

            return null;
        }
    }

    public static class AuthoringToComponentConverter
    {
        private static readonly Dictionary<IClrTypeName, ConversionData> ourAuthoringToComponentDataSimpleTypesConversion = new() 
        {
            {KnownTypes.Vector2, new ConversionData(KnownTypes.Float2, "$0.$1")},
            {KnownTypes.Vector3, new ConversionData(KnownTypes.Float3, "$0.$1")},
        };

        private static readonly ConversionData ourComponentToEntityConversions = new(KnownTypes.Entity, "GetEntity($0.$1, TransformUsageFlags.Dynamic)");

        public static ConversionData? Convert(IClrTypeName clrTypeName, IPsiModule psiModule)
        {
            if (ourAuthoringToComponentDataSimpleTypesConversion.TryGetValue(clrTypeName, out var result))
                return result;
            
            var typeByCLRName = TypeFactory.CreateTypeByCLRName(clrTypeName, NullableAnnotation.Unknown, psiModule);

            var typeElement = typeByCLRName.GetTypeElement();
            if (typeElement.DerivesFrom(KnownTypes.Component) || typeElement.DerivesFrom(KnownTypes.GameObject))
                return ourComponentToEntityConversions;

            return null;
        }
    }

    public static class BakerGeneratorUtils
    {
        public static IType GetFieldType(ITypeOwner selectedField, Func<IClrTypeName,IPsiModule,ConversionData?> converter)
        {
            var fieldTypeName = selectedField.Type.GetTypeElement().NotNull().GetClrName();
            var selectedFieldModule = selectedField.Module;
            var fieldType = TypeFactory.CreateTypeByCLRName(fieldTypeName, NullableAnnotation.NotAnnotated, selectedFieldModule);

            var fieldTypeClrName = fieldType.GetClrName();
            var convertAuthoringToComponentField = converter(fieldTypeClrName, selectedFieldModule);

            if (convertAuthoringToComponentField.HasValue)
                return TypeFactory.CreateTypeByCLRName(convertAuthoringToComponentField.Value.TypeName, NullableAnnotation.NotAnnotated, selectedFieldModule);
            
            return fieldType;
        }
    }

    public readonly struct ConversionData
    {
        public readonly  IClrTypeName TypeName;
        public readonly string FunctionTemplate;

        public ConversionData(IClrTypeName typeName, string functionTemplate)
        {
            TypeName = typeName;
            FunctionTemplate = functionTemplate;
        }
    }
}
