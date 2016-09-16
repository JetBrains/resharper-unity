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
        private readonly bool isStatic;
        private readonly bool returnTypeIsArray;
        [NotNull] private readonly string name;
        [NotNull] private readonly IClrTypeName returnType;
        [NotNull] private readonly UnityMessageParameter[] parameters;

        public UnityMessage([NotNull] string name, [NotNull] IClrTypeName returnType, bool returnTypeIsArray, bool isStatic,
            [NotNull] params UnityMessageParameter[] parameters)
        {
            this.isStatic = isStatic;
            this.name = name;
            this.returnType = returnType;
            this.returnTypeIsArray = returnTypeIsArray;
            this.parameters = parameters.Length > 0 ? parameters : EmptyArray<UnityMessageParameter>.Instance;
        }

        [NotNull]
        public IMethodDeclaration CreateDeclaration([NotNull] CSharpElementFactory factory, [NotNull] IClassLikeDeclaration classDeclaration)
        {
            var builder = new StringBuilder(128);

            builder.Append("private ");
            builder.Append(returnType.FullName);
            if (returnTypeIsArray) builder.Append("[]");
            builder.Append(" ");
            builder.Append(name);
            builder.Append("(");

            for (var i = 0; i < parameters.Length; i++)
            {
                if (i > 0) builder.Append(",");

                var parameter = parameters[i];
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
            if (method.ShortName != name) return false;
            if (method.IsStatic != isStatic) return false;

            var methodReturnType = (IDeclaredType)method.ReturnType;
            if (!Equals(methodReturnType.GetClrName(), returnType)) return false;

            if (method.Parameters.Count != parameters.Length) return false;

            for (var i = 0; i < parameters.Length; ++i)
            {
                var paramType = (IDeclaredType)method.Parameters[i].Type;
                if (!Equals(paramType.GetClrName(), parameters[i].ClrTypeName)) return false;
            }

            return true;
        }
    }
}