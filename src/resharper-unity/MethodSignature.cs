using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
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
            Parameters = new ParameterSignature[parameterTypes.Count];
            for (var i = 0; i < parameterTypes.Count; i++)
            {
                Parameters[i] = new ParameterSignature(parameterNames[i], parameterTypes[i]);
            }
        }

        public IType ReturnType { get; }
        public bool? IsStatic { get; }
        public ParameterSignature[] Parameters { get; }
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
    }
}