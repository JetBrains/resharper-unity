using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public class UnityEventFunction
    {
        private readonly bool myIsStatic;
        private readonly Version myMinimumVersion;
        private readonly Version myMaximumVersion;
        private readonly bool myReturnTypeIsArray;
        [NotNull] private readonly IClrTypeName myReturnType;
        [NotNull] private readonly UnityEventFunctionParameter[] myParameters;

        public UnityEventFunction([NotNull] string name, [NotNull] string typeName, [NotNull] IClrTypeName returnType,
            bool returnTypeIsArray, bool isStatic, string description, bool undocumented,
            Version minimumVersion, Version maximumVersion,
            [NotNull] params UnityEventFunctionParameter[] parameters)
        {
            Description = description;
            Undocumented = undocumented;
            myIsStatic = isStatic;
            myMinimumVersion = minimumVersion;
            myMaximumVersion = maximumVersion;
            Name = name;
            TypeName = typeName;
            myReturnType = returnType;
            myReturnTypeIsArray = returnTypeIsArray;
            myParameters = parameters.Length > 0 ? parameters : EmptyArray<UnityEventFunctionParameter>.Instance;
        }

        [NotNull] public string TypeName { get; }
        [NotNull] public string Name { get; }
        [CanBeNull] public string Description { get; }
        public bool Undocumented { get; }

        [NotNull]
        public IMethodDeclaration CreateDeclaration([NotNull] CSharpElementFactory factory, [NotNull] IClassLikeDeclaration classDeclaration)
        {
            var builder = new StringBuilder(128);

            builder.Append("private ");
            if (myIsStatic) builder.Append("static ");
            builder.Append(myReturnType.FullName);
            if (myReturnTypeIsArray) builder.Append("[]");
            builder.Append(" ");
            builder.Append(Name);
            builder.Append("(");

            for (var i = 0; i < myParameters.Length; i++)
            {
                if (i > 0) builder.Append(",");

                var parameter = myParameters[i];
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

        public bool Match([NotNull] IMethod method)
        {
            if (method.ShortName != Name) return false;
            if (method.IsStatic != myIsStatic) return false;

            if (!DoTypesMatch(method.ReturnType, myReturnType, myReturnTypeIsArray))
                return false;

            if (method.Parameters.Count != myParameters.Length) return false;

            for (var i = 0; i < myParameters.Length; ++i)
            {
                if (!DoTypesMatch(method.Parameters[i].Type, myParameters[i].ClrTypeName, myParameters[i].IsArray))
                    return false;
            }

            return true;
        }

        [CanBeNull]
        public UnityEventFunctionParameter GetParameter(string name)
        {
            return myParameters.FirstOrDefault(p => p.Name == name);
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