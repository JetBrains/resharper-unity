using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api
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
        // Even though ApiXml contains a closed type generic name in CLR format, we parse that, and only use open here
        public IClrTypeName ClrTypeName { get; }
        public bool IsArray { get; }
        public IClrTypeName[] TypeParameters { get; }

        // TODO: Perhaps this should be an extension method
        [NotNull]
        public IType AsIType(KnownTypesCache knownTypesCache, IPsiModule module)
        {
            // TODO: It would be nice to also cache the closed generic and array types
            // We would need a different key for that

            IType type = knownTypesCache.GetByClrTypeName(ClrTypeName, module);
            if (TypeParameters.Length > 0)
            {
                var typeParameters = new IType[TypeParameters.Length];
                for (int i = 0; i < TypeParameters.Length; i++)
                {
                    typeParameters[i] = knownTypesCache.GetByClrTypeName(TypeParameters[i], module);
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
