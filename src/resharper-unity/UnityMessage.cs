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
    public class UnityMessage
    {
        private readonly bool myIsStatic;
        private readonly bool myReturnTypeIsArray;
        [NotNull] private readonly string myName;
        [NotNull] private readonly IClrTypeName myReturnType;
        [NotNull] private readonly UnityMessageParameter[] myParameters;

        public UnityMessage([NotNull] string name, [NotNull] IClrTypeName returnType, bool returnTypeIsArray, bool isStatic, string description,
            [NotNull] params UnityMessageParameter[] parameters)
        {
            Description = description;
            myIsStatic = isStatic;
            myName = name;
            myReturnType = returnType;
            myReturnTypeIsArray = returnTypeIsArray;
            myParameters = parameters.Length > 0 ? parameters : EmptyArray<UnityMessageParameter>.Instance;
        }

        public string Description { get; }

        [NotNull]
        public IMethodDeclaration CreateDeclaration([NotNull] CSharpElementFactory factory, [NotNull] IClassLikeDeclaration classDeclaration)
        {
            var builder = new StringBuilder(128);

            builder.Append("private ");
            builder.Append(myReturnType.FullName);
            if (myReturnTypeIsArray) builder.Append("[]");
            builder.Append(" ");
            builder.Append(myName);
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
            if (method.ShortName != myName) return false;
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
        public UnityMessageParameter GetParameter(string name)
        {
            return myParameters.FirstOrDefault(p => p.Name == name);
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