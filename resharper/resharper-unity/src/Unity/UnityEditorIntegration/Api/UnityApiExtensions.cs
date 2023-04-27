#nullable enable
using System.Collections.Generic;
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
        private static readonly IClrTypeName[] ourDotsTypes =
        {
            KnownTypes.ComponentSystemBase,
            KnownTypes.ISystem,
            KnownTypes.IAspect,
            KnownTypes.IComponentData,
            KnownTypes.IJobEntity,
            KnownTypes.IBaker,
        };

        public static bool IsDotsImplicitlyUsedType([NotNullWhen(true)] this ITypeElement? typeElement)
        {
            return typeElement.GetDotsCLRBaseTypeName() != null;
        }

        public static IClrTypeName? GetDotsCLRBaseTypeName(this ITypeElement? typeElement)
        {
            IClrTypeName clrBaseTypeName = null;
            foreach (var baseClrTypeName in ourDotsTypes)
            {
                if (!typeElement.DerivesFrom(baseClrTypeName)) continue;
                clrBaseTypeName = baseClrTypeName;
                break;
            }

            return clrBaseTypeName;
        }


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