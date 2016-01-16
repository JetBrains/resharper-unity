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
    public class MonoBehaviourEvent
    {
        public bool IsStatic { get; }

        [NotNull]
        public string Name { get; }

        [NotNull]
        public IClrTypeName Type { get; }

        [NotNull]
        public MonoBehaviourEventParameter[] Parameters { get; }

        public bool ReturnsArray { get; }

        public MonoBehaviourEvent([NotNull] string name, [NotNull] IClrTypeName type, bool returnsArray, bool isStatic, [NotNull] params MonoBehaviourEventParameter[] parameters)
        {
            IsStatic = isStatic;
            Name = name;
            Type = type;
            ReturnsArray = returnsArray;
            Parameters = parameters.Length > 0 ? parameters : EmptyArray<MonoBehaviourEventParameter>.Instance;
        }

        [NotNull]
        public IMethodDeclaration CreateDeclaration([NotNull] CSharpElementFactory factory, [NotNull] IClassLikeDeclaration classDeclaration)
        {
            var builder = new StringBuilder(128);

            builder.Append("private ");
            builder.Append(Type.FullName);
            builder.Append(" ");
            builder.Append(Name);
            builder.Append("(");

            for (var i = 0; i < Parameters.Length; i++)
            {
                if (i > 0) builder.Append(",");

                MonoBehaviourEventParameter parameter = Parameters[i];
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
            if (method.IsStatic != IsStatic) return false;

            var returnType = (IDeclaredType)method.ReturnType;
            if (!Equals(returnType.GetClrName(), Type)) return false;

            if (method.Parameters.Count != Parameters.Length) return false;

            for (var i = 0; i < Parameters.Length; ++i)
            {
                var paramType = (IDeclaredType)method.Parameters[i].Type;
                if (!Equals(paramType.GetClrName(), Parameters[i].ClrTypeName)) return false;
            }

            return true;
        }
    }

    public class MonoBehaviourEventParameter
    {
        [NotNull]
        public string Name { get; }

        [NotNull]
        public IClrTypeName ClrTypeName { get; }

        public bool IsArray { get; }
        
        public MonoBehaviourEventParameter([NotNull] string name, [NotNull] IClrTypeName clrTypeName, bool isArray = false)
        {
            Name = name;
            ClrTypeName = clrTypeName;
            IsArray = isArray;
        }
    }
}