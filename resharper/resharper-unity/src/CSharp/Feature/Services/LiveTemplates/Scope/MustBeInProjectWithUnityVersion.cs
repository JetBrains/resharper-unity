using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    // If the scope point declares itself mandatory, then a template is unavailable unless the mandatory scope point is
    // valid for the current context. E.g. a file template declares it is available for an InUnityProjectVersion(2017.3)
    // scope point. If this mandatory scope point is not in the current context, the template is unavailable
    public class MustBeInProjectWithUnityVersion : InAnyFile, IMandatoryScopePoint
    {
        // Serialised name, so don't use nameof
        public const string TypeName = "MustBeInProjectWithUnityVersion";
        public const string VersionProperty = "version";

        private static readonly Guid ourDefaultUID = new Guid("7FB2BDA8-3264-4E6A-B319-4343C6F8A1F4");

        private readonly Version myActualVersion;

        public MustBeInProjectWithUnityVersion(Version minimumVersion)
        {
            myActualVersion = minimumVersion;
        }

        public override Guid GetDefaultUID() => ourDefaultUID;

        public override string PresentableShortName => $"Unity {myActualVersion} and later projects";

        // The real scope point is called, and passed in the allowed scope point from the template definition
        public override bool IsSubsetOf(ITemplateScopePoint allowed)
        {
            if (!base.IsSubsetOf(allowed))
                return false;

            if (allowed is MustBeInProjectWithUnityVersion allowedScopePoint)
            {
                var allowedVersion = allowedScopePoint.myActualVersion;
                return allowedVersion <= myActualVersion;
            }

            return true;
        }

        public override IEnumerable<Pair<string, string>> EnumerateCustomProperties()
        {
            yield return new Pair<string, string>(VersionProperty, myActualVersion.ToString(3));
        }

        public override string ToString()
        {
            return $"Unity minimum version: {myActualVersion}";
        }
    }
}