using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Colors
{
    public class UnityColorTypes
    {
        private static readonly Key<UnityColorTypes> ourColorTypesKey = new Key<UnityColorTypes>("UnityColorTypes");
        private static readonly IClrTypeName UnityColorTypeName = new ClrTypeName("UnityEngine.Color");
        private static readonly IClrTypeName UnityColor32TypeName = new ClrTypeName("UnityEngine.Color32");

        public static UnityColorTypes GetInstance(IPsiModule module)
        {
            var unityColorTypes = module.GetData(ourColorTypesKey);
            if (unityColorTypes == null)
            {
                unityColorTypes = new UnityColorTypes(module);
                module.PutData(ourColorTypesKey, unityColorTypes);
            }

            return unityColorTypes;
        }

        private UnityColorTypes([NotNull] IPsiModule module)
        {
            var cache = module.GetPsiServices().Symbols.GetSymbolScope(module, true, true);

            UnityColorType = cache.GetTypeElementByCLRName(UnityColorTypeName);
            UnityColor32Type = cache.GetTypeElementByCLRName(UnityColor32TypeName);
        }

        [CanBeNull] public ITypeElement UnityColorType { get; }
        [CanBeNull] public ITypeElement UnityColor32Type { get; }

        public bool IsUnityColorType([CanBeNull] ITypeElement typeElement)
        {
            return (UnityColorType != null && UnityColorType.Equals(typeElement))
                || (UnityColor32Type != null && UnityColor32Type.Equals(typeElement));
        }

        public bool IsUnityColorTypeSupportingProperties([CanBeNull] ITypeElement typeElement)
        {
            return UnityColorType != null && UnityColorType.Equals(typeElement);
        }

        public bool IsUnityColorTypeSupportingHSV([CanBeNull] ITypeElement typeElement)
        {
            return UnityColorType != null && UnityColorType.Equals(typeElement);
        }

        public static bool IsColorProperty(ITypeMember typeMember)
        {
            if (typeMember is IProperty && typeMember.IsStatic)
            {
                var unityColorTypes = GetInstance(typeMember.Module);
                return unityColorTypes.IsUnityColorTypeSupportingProperties(typeMember.GetContainingType())
                       && UnityNamedColors.Get(typeMember.ShortName).HasValue;
            }

            return false;
        }

        public static Pair<ITypeElement, ITypeMember>? PropertyFromColorElement(ITypeElement qualifierType, IColorElement colorElement, IPsiModule module)
        {
            var colorName = UnityNamedColors.GetColorName(colorElement.RGBColor);
            if (string.IsNullOrEmpty(colorName))
                return null;

            var unityColorType = GetInstance(module).UnityColorType;
            if (unityColorType == null || !unityColorType.Equals(qualifierType)) return null;

            var colorProperties = GetStaticColorProperties(unityColorType);
            var propertyTypeMember = colorProperties.FirstOrDefault(p => p.ShortName == colorName);
            if (propertyTypeMember == null) return null;

            return Pair.Of(unityColorType, propertyTypeMember);
        }

        private static IList<ITypeMember> GetStaticColorProperties(ITypeElement unityColorType)
        {
            var colorProperties = new LocalList<ITypeMember>();

            foreach (var typeMember in unityColorType.GetMembers())
            {
                if (!typeMember.IsStatic) continue;

                var typeOwner = typeMember as ITypeOwner;
                if (typeOwner is IProperty || typeOwner is IField)
                {
                    var declaredType = typeOwner.Type as IDeclaredType;
                    if (declaredType != null && unityColorType.Equals(declaredType.GetTypeElement()))
                    {
                        colorProperties.Add(typeMember);
                    }
                }
            }

            return colorProperties.ResultingList();
        }
    }
}