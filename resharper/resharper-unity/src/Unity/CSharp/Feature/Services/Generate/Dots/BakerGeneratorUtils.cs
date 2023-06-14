#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
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
        [Flags]
        public enum AddComponentMethodType
        {
            EmptyComponent = 1 << 0,
            ComponentWithInitialization = 1 << 1,
            Both = EmptyComponent | ComponentWithInitialization
        }
        
        public static IType GetFieldType(ITypeOwner selectedField, Func<IClrTypeName, IPsiModule, ConversionData?> converter)
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

        public static TreeNodeActionType FindIBakerAddComponentExpression(ITreeNode node,
            ITypeElement componentDeclaredType, AddComponentMethodType addComponentMethodType = AddComponentMethodType.Both)
        {
            if (node is IMethodDeclaration)
                return TreeNodeActionType.IGNORE_SUBTREE;

            if (node is not IInvocationExpression invocationExpression)
                return TreeNodeActionType.CONTINUE;

            if (!invocationExpression.IsIBakerAddComponentMethod())
                return TreeNodeActionType.CONTINUE;

            var arguments = invocationExpression.ArgumentList.Arguments;
            if (arguments.Count == 0)
                return TreeNodeActionType.CONTINUE;

            var firstArgumentName = arguments[0].MatchingParameter.Element.ShortName;

            if (!firstArgumentName.Equals("entity"))
                return TreeNodeActionType.CONTINUE;

            var typeArguments = invocationExpression.TypeArguments;

            //if AddComponent(entity, new Component(){})
            if (typeArguments.Count == 0 &&  addComponentMethodType.HasFlag(AddComponentMethodType.ComponentWithInitialization))
            {
                if (arguments.Count != 2)
                    return TreeNodeActionType.CONTINUE;

                var matchingParameterType = arguments[1].MatchingParameter.Type.GetTypeElement();
                return componentDeclaredType.Equals(matchingParameterType)
                    ? TreeNodeActionType.ACCEPT
                    : TreeNodeActionType.CONTINUE;
            }                
                
            
            //check if AddComponent<Component>(entity)
            if (typeArguments.Count == 1 && addComponentMethodType.HasFlag(AddComponentMethodType.EmptyComponent) &&
                componentDeclaredType.Equals(typeArguments[0].GetTypeElement()))
                return TreeNodeActionType.ACCEPT;
            
            return TreeNodeActionType.CONTINUE;
        }

        public static TreeNodeActionType FindIBakerAddComponentObjectExpression(ITreeNode node,
            ITypeElement componentDeclaredType)
        {
            
            if (node is IMethodDeclaration)
                return TreeNodeActionType.IGNORE_SUBTREE;

            if (node is not IInvocationExpression invocationExpression)
                return TreeNodeActionType.CONTINUE;

            if (!invocationExpression.IsIBakerAddComponentObjectMethod())
                return TreeNodeActionType.CONTINUE;
            
            var arguments = invocationExpression.ArgumentList.Arguments;
            if (arguments.Count == 0)
                return TreeNodeActionType.CONTINUE;

            var firstArgumentName = arguments[0].MatchingParameter.Element.ShortName;

            if (!firstArgumentName.Equals("entity"))
                return TreeNodeActionType.CONTINUE;

            //if AddComponentObject(entity, new Component(){})
            if (arguments.Count != 2)
                return TreeNodeActionType.CONTINUE;

            var matchingParameterType = arguments[1].MatchingParameter.Type.GetTypeElement();
            return componentDeclaredType.Equals(matchingParameterType)
                ? TreeNodeActionType.ACCEPT
                : TreeNodeActionType.CONTINUE;

        }

        public static TreeNodeActionType FindAddComponentCreationExpression(ITreeNode node,
            ITypeElement componentDeclaredType)
        {
            if (node is IMethodDeclaration)
                return TreeNodeActionType.IGNORE_SUBTREE;

            if (node is not IObjectCreationExpression objectCreationExpression)
                return TreeNodeActionType.CONTINUE;

            if (!componentDeclaredType.Equals(objectCreationExpression.Type().GetTypeElement()))
                return TreeNodeActionType.CONTINUE;

            var parentInvocationExpression = objectCreationExpression.GetContainingNode<IInvocationExpression>();

            if (parentInvocationExpression == null)
                return TreeNodeActionType.CONTINUE;

            if (!parentInvocationExpression.IsIBakerAddComponentMethod() &&
                !parentInvocationExpression.IsIBakerAddComponentObjectMethod())
                return TreeNodeActionType.CONTINUE;

            var arguments = parentInvocationExpression.ArgumentList.Arguments;
            if (arguments.Count == 0)
                return TreeNodeActionType.CONTINUE;

            var firstArgumentName = arguments[0].MatchingParameter.Element.ShortName;

            if (!firstArgumentName.Equals("entity"))
                return TreeNodeActionType.CONTINUE;

            //if AddComponentObject(entity, new Component(){})
            if (arguments.Count != 2)
                return TreeNodeActionType.CONTINUE;

            var matchingParameterType = arguments[1].MatchingParameter.Type.GetTypeElement();
            return componentDeclaredType.Equals(matchingParameterType)
                ? TreeNodeActionType.ACCEPT
                : TreeNodeActionType.CONTINUE;
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
