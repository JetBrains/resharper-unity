using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public class MethodSignature
    {
        public MethodSignature(IType returnType, bool? isStatic)
            : this(returnType, isStatic, EmptyArray<IType>.Instance, EmptyArray<string>.Instance)
        {
        }

        public MethodSignature(IType returnType, bool? isStatic, IReadOnlyList<IType> parameterTypes, IReadOnlyList<string> parameterNames)
        {
            ReturnType = returnType;
            IsStatic = isStatic;
            var parameters = new ParameterSignature[parameterTypes.Count];
            for (var i = 0; i < parameterTypes.Count; i++)
                parameters[i] = new ParameterSignature(parameterNames[i], parameterTypes[i]);
            Parameters = new Parameters(parameters);
        }

        public IType ReturnType { get; }
        public bool? IsStatic { get; }
        public Parameters Parameters { get; }

        public string FormatSignature(string methodName)
        {
            return $"{GetReturnTypeName()} {methodName}({Parameters.GetParameterTypes()})";
        }

        public string GetReturnTypeName()
        {
            return ReturnType.GetPresentableName(CSharpLanguage.Instance);
        }
    }

    public class Parameters
    {
        private readonly ParameterSignature[] myParameters;

        public Parameters(ParameterSignature[] parameters)
        {
            myParameters = parameters;
        }

        public ParameterSignature this[int i] => myParameters[i];
        public int Length => myParameters.Length;

        public string GetParameterList()
        {
            return string.Join(", ", myParameters.Select(p => p.ToString()));
        }

        public string GetParameterTypes()
        {
            return string.Join(", ", myParameters.Select(p => p.GetTypeName()));
        }
    }

    public class ParameterSignature
    {
        public ParameterSignature(string name, IType type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public IType Type { get; }

        public string GetTypeName()
        {
            return Type.GetPresentableName(CSharpLanguage.Instance);
        }

        public override string ToString()
        {
            return $"{GetTypeName()} {Name}";
        }
    }

    public static class MethodSignatureExtensions
    {
        public static MethodSignature AsMethodSignature(this UnityEventFunction eventFunction, IPsiModule module)
        {
            IType returnType = TypeFactory.CreateTypeByCLRName(eventFunction.ReturnType, module);
            if (eventFunction.ReturnTypeIsArray)
                returnType = TypeFactory.CreateArrayType(returnType, 1);

            if (eventFunction.Parameters.Length == 0)
                return new MethodSignature(returnType, eventFunction.IsStatic);

            var parameterTypes = new IType[eventFunction.Parameters.Length];
            var parameterNames = new string[eventFunction.Parameters.Length];
            for (var i = 0; i < eventFunction.Parameters.Length; i++)
            {
                var parameter = eventFunction.Parameters[i];
                IType paramType = TypeFactory.CreateTypeByCLRName(parameter.ClrTypeName, module);
                if (parameter.IsArray)
                    paramType = TypeFactory.CreateArrayType(paramType, 1);
                parameterTypes[i] = paramType;
                parameterNames[i] = parameter.Name;
            }

            return new MethodSignature(returnType, eventFunction.IsStatic, parameterTypes, parameterNames);
        }

        public static bool HasMatchingStaticModifier(this MethodSignature methodSignature,
            IMethodDeclaration methodDeclaration)
        {
            if (!methodSignature.IsStatic.HasValue)
                return true;
            return methodSignature.IsStatic.Value == methodDeclaration.IsStatic;
        }

        public static bool HasMatchingReturnType(this MethodSignature methodSignature,
            IMethodDeclaration methodDeclaration)
        {
            return Equals(methodSignature.ReturnType, methodDeclaration.Type);
        }

        public static bool HasMatchingTypeParameters(this MethodSignature methodSignature,
            IMethodDeclaration methodDeclaration)
        {
            // We don't have any generic methods. So it's an error if anyone has any
            return !methodDeclaration.TypeParameterDeclarations.Any();
        }

        public static bool HasMatchingParameters(this MethodSignature methodSignature,
            IMethodDeclaration methodDeclaration)
        {
            var parameters = methodDeclaration.ParameterDeclarations;
            if (methodSignature.Parameters.Length != parameters.Count)
                return false;

            for (var i = 0; i < parameters.Count; i++)
            {
                if (!Equals(methodSignature.Parameters[i].Type, parameters[i].Type))
                    return false;
            }

            return true;
        }
    }
}