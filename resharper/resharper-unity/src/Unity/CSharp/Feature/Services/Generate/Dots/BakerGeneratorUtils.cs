#nullable enable
using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots
{
    public static class BakerGeneratorUtils
    {
        private static readonly Dictionary<IClrTypeName, ConversionData> ourComponentDataToAuthoringTypesConversion = new() 
        {
            {KnownTypes.Entity, new ConversionData(KnownTypes.GameObject, "GetEntity($0.$1)")},
            {KnownTypes.Random, new ConversionData(PredefinedType.UINT_FQN, "Unity.Mathematics.Random.CreateFromIndex($0.$1)")}
        };
        
        public static ConversionData? ConvertComponentToAuthoringField(IClrTypeName clrTypeName, IPsiModule psiModule)
        {
            if (ourComponentDataToAuthoringTypesConversion.TryGetValue(clrTypeName, out var result))
                return result;
           

            return null;
        }

        private static readonly Dictionary<IClrTypeName, ConversionData> ourAuthoringToComponentDataSimpleTypesConversion = new() 
        {
            {KnownTypes.Vector2, new ConversionData(KnownTypes.Float2, "$0.$1")},
            {KnownTypes.Vector3, new ConversionData(KnownTypes.Float3, "$0.$1")},
        };

        private static readonly ConversionData ourComponentToEntityConversions = new(KnownTypes.Entity, "GetEntity($0.$1)");
        
        public static ConversionData? ConvertAuthoringToComponentField(IClrTypeName clrTypeName, IPsiModule psiModule)
        {
            if (ourAuthoringToComponentDataSimpleTypesConversion.TryGetValue(clrTypeName, out var result))
                return result;
            
            var typeByCLRName = TypeFactory.CreateTypeByCLRName(clrTypeName, NullableAnnotation.Unknown, psiModule);

            var typeElement = typeByCLRName.GetTypeElement();
            if (typeElement.DerivesFrom(KnownTypes.Component) || typeElement.DerivesFrom(KnownTypes.GameObject))
                return ourComponentToEntityConversions;

            return null;
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
}