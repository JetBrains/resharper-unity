using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class UnityTypeUtils
    {
        [NotNull]
        public static IEnumerable<UnityType> GetBaseUnityTypes([CanBeNull] ITypeElement type,
            UnityVersion unityVersion, UnityTypesProvider unityTypes, KnownTypesCache knownTypesCache)
        {
            if (type?.Module is IProjectPsiModule projectPsiModule)
            {
                var version = unityVersion.GetActualVersion(projectPsiModule.Project);
                return GetBaseUnityTypes(type, version, unityTypes, knownTypesCache);
            }

            return EmptyArray<UnityType>.Instance;
        }

        [NotNull]
        public static IEnumerable<UnityType> GetBaseUnityTypes([NotNull] ITypeElement type, Version unityVersion,
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