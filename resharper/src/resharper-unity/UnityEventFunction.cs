using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public class UnityEventFunction
    {
        private static readonly IClrTypeName EnumeratorType = new ClrTypeName("System.Collections.IEnumerator");

        private readonly Version myMinimumVersion;
        private readonly Version myMaximumVersion;

        public UnityEventFunction([NotNull] string name, [NotNull] string typeName, [NotNull] IClrTypeName returnType, bool returnTypeIsArray, bool isStatic, bool isCoroutine, string description, bool undocumented, Version minimumVersion, Version maximumVersion, [NotNull] params UnityEventFunctionParameter[] parameters)
        {
            Description = description;
            Undocumented = undocumented;
            IsStatic = isStatic;
            Coroutine = isCoroutine;
            myMinimumVersion = minimumVersion;
            myMaximumVersion = maximumVersion;
            Name = name;
            TypeName = typeName;
            ReturnType = returnType;
            ReturnTypeIsArray = returnTypeIsArray;
            Parameters = parameters.Length > 0 ? parameters : EmptyArray<UnityEventFunctionParameter>.Instance;
        }

        [NotNull] public string TypeName { get; }
        [NotNull] public string Name { get; }
        [NotNull] public UnityEventFunctionParameter[] Parameters { get; }
        [NotNull] public IClrTypeName ReturnType { get; }
        public bool ReturnTypeIsArray { get; }
        public bool Coroutine { get; }
        public bool IsStatic { get; }
        [CanBeNull] public string Description { get; }
        public bool Undocumented { get; }

        [NotNull]
        public IMethodDeclaration CreateDeclaration([NotNull] CSharpElementFactory factory, [NotNull] IClassLikeDeclaration classDeclaration)
        {
            var builder = new StringBuilder(128);

            builder.Append("private ");
            if (IsStatic) builder.Append("static ");
            builder.Append(ReturnType.FullName);
            if (ReturnTypeIsArray) builder.Append("[]");
            builder.Append(" ");
            builder.Append(Name);
            builder.Append("(");

            for (var i = 0; i < Parameters.Length; i++)
            {
                if (i > 0) builder.Append(",");

                var parameter = Parameters[i];
                // TODO: `out` or `ref`?
                // From reflection point of view, it's a "ByRef" Type, and that's all we know...
                // The only place it's currently being used is an out parameter
                if (parameter.IsByRef) builder.Append("out ");
                builder.Append(parameter.ClrTypeName.FullName);
                if (parameter.IsArray) builder.Append("[]");
                builder.Append(' ');
                builder.Append(parameter.Name);
            }

            builder.Append(");");

            var declaration = (IMethodDeclaration)factory.CreateTypeMemberDeclaration(builder.ToString());
            declaration.SetResolveContextForSandBox(classDeclaration, SandBoxContextType.Child);
            declaration.FormatNode();
            return declaration;
        }

        public EventFunctionMatch Match([NotNull] IMethod method)
        {
            if (method.ShortName != Name) return EventFunctionMatch.NoMatch;

            var match = EventFunctionMatch.MatchingName;
            if (method.IsStatic == IsStatic)
                match |= EventFunctionMatch.MatchingStaticModifier;

            var matchingSignature = false;
            if (method.Parameters.Count == Parameters.Length)
            {
                matchingSignature = true;
                for (var i = 0; i < Parameters.Length && matchingSignature; i++)
                {
                    if (!DoTypesMatch(method.Parameters[i].Type, Parameters[i].ClrTypeName,
                        Parameters[i].IsArray))
                    {
                        matchingSignature = false;
                    }
                }
            }
            else
            {
                // TODO: This doesn't really handle optional parameters very well
                // It's fine for the current usage (a single parameter, either there or not)
                // but won't work for anything more interesting. Perhaps optional parameters
                // should be modeled as overloads?
                var optionalParameters = 0;
                foreach (var parameter in Parameters)
                {
                    if (parameter.IsOptional)
                        optionalParameters++;
                }
                if (method.Parameters.Count + optionalParameters == Parameters.Length)
                    matchingSignature = true;
            }

            if (matchingSignature)
                match |= EventFunctionMatch.MatchingSignature;

            if (DoTypesMatch(method.ReturnType, ReturnType, ReturnTypeIsArray)
                || (Coroutine && DoTypesMatch(method.ReturnType, EnumeratorType, false)))
            {
                match |= EventFunctionMatch.MatchingReturnType;
            }

            if (method.TypeParameters.Count == 0)
                match |= EventFunctionMatch.MatchingTypeParameters;

            return match;
        }

        [CanBeNull]
        public UnityEventFunctionParameter GetParameter(string name)
        {
            return Parameters.FirstOrDefault(p => p.Name == name);
        }

        public bool SupportsVersion(Version unityVersion)
        {
            return myMinimumVersion <= unityVersion && unityVersion <= myMaximumVersion;
        }

        private static bool DoTypesMatch(IType type, IClrTypeName expectedTypeName, bool isArray)
        {
            IDeclaredType declaredType;

            var arrayType = type as IArrayType;
            if (arrayType != null)
            {
                if (!isArray) return false;

                // TODO: Does this handle multi-dimensional arrays? Do we care?
                declaredType = arrayType.GetScalarType();
            }
            else
            {
                declaredType = (IDeclaredType) type;
            }

            return declaredType != null && Equals(declaredType.GetClrName(), expectedTypeName);
        }
    }
}