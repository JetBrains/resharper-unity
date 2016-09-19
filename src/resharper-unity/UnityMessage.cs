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

        public UnityMessage([NotNull] string name, [NotNull] IClrTypeName returnType, bool returnTypeIsArray, bool isStatic,
            [NotNull] params UnityMessageParameter[] parameters)
        {
            myIsStatic = isStatic;
            myName = name;
            myReturnType = returnType;
            myReturnTypeIsArray = returnTypeIsArray;
            myParameters = parameters.Length > 0 ? parameters : EmptyArray<UnityMessageParameter>.Instance;
        }

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

            var methodReturnType = (IDeclaredType)method.ReturnType;
            if (!Equals(methodReturnType.GetClrName(), myReturnType)) return false;

            if (method.Parameters.Count != myParameters.Length) return false;

            for (var i = 0; i < myParameters.Length; ++i)
            {
                var paramType = (IDeclaredType)method.Parameters[i].Type;
                if (!Equals(paramType.GetClrName(), myParameters[i].ClrTypeName)) return false;
            }

            return true;
        }
    }
}