using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    // Be very careful about caching this. The IType can easily become invalid. It's ok to cache for highlightings and
    // references, as the daemon will invalidate correctly. Call IsValid to be sure.
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
        public bool? IsStatic { get; }    // Null means usage ignores static modifier
        public Parameters Parameters { get; }

        public bool IsValid() => ReturnType.IsValid() && Parameters.IsValid();

        public string FormatSignature(string methodName)
        {
            var modifier = IsStatic == true ? "static " : string.Empty;
            return $"{modifier}{GetReturnTypeName()} {methodName}({Parameters.GetParameterTypes()})";
        }

        // ReSharper disable once AssignNullToNotNullAttribute
        public string GetReturnTypeName() => ReturnType.GetPresentableName(CSharpLanguage.Instance);
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

        public bool IsValid() => myParameters.All(p => p.IsValid());

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

        public bool IsValid() => Type.IsValid();
        // ReSharper disable once AssignNullToNotNullAttribute
        public string GetTypeName() => Type.GetPresentableName(CSharpLanguage.Instance);
        public override string ToString() => $"{GetTypeName()} {Name}";
    }
}