#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api
{
    public static class UnityTypeUtils
    {
        public static bool IsUnityObject(IType type) => !type.IsUnknown && type.GetTypeElement() is { } typeElement && typeElement.DerivesFrom(KnownTypes.Object);

        public static IEnumerable<UnityType> GetBaseUnityTypes(ITypeElement? type,
            UnityVersion unityVersion, UnityTypesProvider unityTypes, KnownTypesCache knownTypesCache)
        {
            if (type?.Module is IProjectPsiModule projectPsiModule)
            {
                var version = unityVersion.GetActualVersion(projectPsiModule.Project);
                return GetBaseUnityTypes(type, version, unityTypes, knownTypesCache);
            }

            return EmptyArray<UnityType>.Instance;
        }

        public static IEnumerable<UnityType> GetBaseUnityTypes(ITypeElement type, Version unityVersion,
            UnityTypesProvider unityTypes, KnownTypesCache knownTypesCache)
        {
            unityVersion = unityTypes.Types.NormaliseSupportedVersion(unityVersion);
            return GetBaseUnityTypes(unityTypes, type, unityVersion, knownTypesCache);
        }

        public static IEnumerable<UnityType> GetBaseUnityTypes(UnityTypesProvider typesProvider, ITypeElement type,
            Version normalisedVersion, KnownTypesCache knownTypesCache)
        {
            return typesProvider.Types.Types.Where(t =>
            {
                using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
                    return t.SupportsVersion(normalisedVersion) && type.IsDescendantOf(t.GetTypeElement(knownTypesCache, type.Module));
            });
        }
    }
}