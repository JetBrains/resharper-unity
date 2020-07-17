using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public class UnityTypeSpec
    {
        public static UnityTypeSpec Void = new UnityTypeSpec(PredefinedType.VOID_FQN);

        public UnityTypeSpec(IClrTypeName typeName, bool isArray = false, IClrTypeName[] typeParameters = null)
        {
           ClrTypeName = typeName;
           IsArray = isArray;
           TypeParameters = typeParameters ?? EmptyArray<IClrTypeName>.Instance;
        }

        // CLR type name, not C# type name. Mostly the same, but can only represent open generics
        // E.g. System.Collections.Generics.List`1
        public IClrTypeName ClrTypeName { get; }
        public bool IsArray { get; }
        public IClrTypeName[] TypeParameters { get; }

        // TODO: Perhaps this should be an extension method
        [NotNull]
        public IType AsIType(IPsiModule module)
        {
            IType type = TypeFactory.CreateTypeByCLRName(ClrTypeName, module);
            if (TypeParameters.Length > 0)
            {
                var typeParameters = new IType[TypeParameters.Length];
                for (int i = 0; i < TypeParameters.Length; i++)
                {
                    typeParameters[i] = TypeFactory.CreateTypeByCLRName(TypeParameters[i], module);
                }

                var typeElement = type.GetTypeElement().NotNull("typeElement != null");
                type = TypeFactory.CreateType(typeElement, typeParameters);
            }

            if (IsArray)
                type = TypeFactory.CreateArrayType(type, 1);

            return type;
        }
    }
}
