#nullable enable
using System.Diagnostics.CodeAnalysis;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api
{
    public static class UnityApiExtensions
    {
        public static bool IsDotsImplicitlyUsedType([NotNullWhen(true)] this ITypeElement? typeElement) =>
            typeElement.DerivesFrom(KnownTypes.ComponentSystemBase)
            || typeElement.DerivesFrom(KnownTypes.ISystem)
            || typeElement.DerivesFrom(KnownTypes.IAspect)
            || typeElement.DerivesFrom(KnownTypes.IComponentData)
            || typeElement.DerivesFrom(KnownTypes.IJobEntity)
            || typeElement.DerivesFrom(KnownTypes.IBaker);


        public static (ITypeElement?, bool) GetReferencedType(IFieldDeclaration? fieldDeclaration)
        {
            if (fieldDeclaration == null)
                return (null, false);

            var (fieldTypeElement, substitution) = fieldDeclaration.DeclaredElement?.Type as IDeclaredType;

            if (fieldTypeElement == null)
                return (null, false);

            var isRefRo = fieldTypeElement.IsClrName(KnownTypes.RefRO);
            var isRefRw = fieldTypeElement.IsClrName(KnownTypes.RefRW);
            var isRef = isRefRo || isRefRw;

            if (!isRef)
            {
                if (fieldTypeElement.DerivesFrom(KnownTypes.IAspect))
                    return (fieldTypeElement, false);
                
                return (null, false);
            }
           
            var refTypeParameter = fieldTypeElement.TypeParameters[0];
            var internalType = substitution[refTypeParameter];

            var referencedTypeElement = internalType.GetTypeElement();
            return (referencedTypeElement, isRefRo);
        }
    }
}