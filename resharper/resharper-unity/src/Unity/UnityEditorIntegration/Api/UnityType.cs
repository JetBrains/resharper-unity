using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.VersionUtils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api
{
    public class UnityType
    {
        private readonly IClrTypeName myTypeName;
        private readonly Version myMinimumVersion;
        private readonly Version myMaximumVersion;
        private readonly IEnumerable<UnityEventFunction> myEventFunctions;

        public UnityType(IClrTypeName typeName, IEnumerable<UnityEventFunction> eventFunctions, Version minimumVersion, Version maximumVersion)
        {
            myTypeName = typeName;
            myMinimumVersion = minimumVersion;
            myMaximumVersion = maximumVersion;
            myEventFunctions = eventFunctions;
        }

        public IEnumerable<UnityEventFunction> GetEventFunctions(Version normalisedUnityVersion)
        {
            return myEventFunctions.Where(f => f.SupportsVersion(normalisedUnityVersion));
        }

        [CanBeNull]
        public ITypeElement GetTypeElement(KnownTypesCache knownTypesCache, [NotNull] IPsiModule module)
        {
            using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
            {
                var type = knownTypesCache.GetByClrTypeName(myTypeName, module);
                return type.GetTypeElement();
            }
        }

        public bool HasEventFunction([NotNull] IMethod method, Version normalisedUnityVersion)
        {
            foreach (var function in myEventFunctions)
            {
                if (function.SupportsVersion(normalisedUnityVersion) && function.Match(method) != MethodSignatureMatch.NoMatch)
                    return true;
            }
            return false;
        }

        public bool SupportsVersion(Version unityVersion)
        {
            return myMinimumVersion.CompareToLenient(unityVersion) <= 0
                   && unityVersion.CompareToLenient(myMaximumVersion) <= 0;
        }
    }
}