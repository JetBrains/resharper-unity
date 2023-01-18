using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    // If the scope point declares itself mandatory, then a template is unavailable unless the mandatory scope point is
    // valid for the current context. E.g. a file template declares it is available for an InUnityProjectVersion(2017.3)
    // scope point. If this mandatory scope point is not in the current context, the template is unavailable
    public class MustBeInProjectWithMinimumUnityVersion : InAnyFile, IMandatoryScopePoint
    {
        // Serialised name, so don't use nameof
        public const string TypeName = "MustBeInProjectWithUnityVersion";
        public const string VersionProperty = "version";

        private static readonly Guid ourDefaultUID = new Guid("7FB2BDA8-3264-4E6A-B319-4343C6F8A1F4");

        public readonly Version ActualVersion;

        public MustBeInProjectWithMinimumUnityVersion(Version minimumVersion)
        {
            ActualVersion = minimumVersion;
        }

        public override Guid GetDefaultUID() => ourDefaultUID;

        public override string PresentableShortName => $"Unity {ActualVersion} and later projects";

        // The real scope point is called, and passed in the allowed scope point from the template definition
        public override bool IsSubsetOf(ITemplateScopePoint allowed)
        {
            return false;
        }

        public override IEnumerable<Pair<string, string>> EnumerateCustomProperties()
        {
            yield return new Pair<string, string>(VersionProperty, ActualVersion.ToString());
        }

        public override string ToString()
        {
            return $"Unity minimum version: {ActualVersion}";
        }
    }
    
    public class MustBeInProjectWithMaximumUnityVersion : InAnyFile, IMandatoryScopePoint
    {
        // Serialised name, so don't use nameof
        public const string TypeName = "MustBeInProjectWithMaximumUnityVersion";
        public const string VersionProperty = "version";

        private static readonly Guid ourDefaultUID = new Guid("56B1EEE1-AE95-49E8-91D1-234000CA35F1");

        public readonly Version ActualVersion;

        public MustBeInProjectWithMaximumUnityVersion(Version maximumVersion)
        {
            ActualVersion = maximumVersion;
        }

        public override Guid GetDefaultUID() => ourDefaultUID;

        public override string PresentableShortName => $"Before Unity {ActualVersion}";

        // The real scope point is called, and passed in the allowed scope point from the template definition
        public override bool IsSubsetOf(ITemplateScopePoint allowed)
        {
            return false;
        }

        public override IEnumerable<Pair<string, string>> EnumerateCustomProperties()
        {
            yield return new Pair<string, string>(VersionProperty, ActualVersion.ToString());
        }

        public override string ToString()
        {
            return $"Unity maximum version: {ActualVersion}";
        }
    }
    
    // Not used in UI, similar to InProjectWithReference
    public class MustBeInProjectWithCurrentUnityVersion : InAnyFile, IMandatoryScopePoint
    {
        public const string VersionProperty = "version";

        private static readonly Guid ourDefaultUID = new Guid("5C48F51C-0E52-46F8-8102-7D28B6F97EBB");

        public readonly Version ActualVersion;

        public MustBeInProjectWithCurrentUnityVersion(Version maximumVersion)
        {
            ActualVersion = maximumVersion;
        }

        public override Guid GetDefaultUID() => ourDefaultUID;

        public override string PresentableShortName => $"Current Unity {ActualVersion}";

        public override bool IsSubsetOf(ITemplateScopePoint allowed)
        {
            if (allowed is MustBeInProjectWithMinimumUnityVersion allowedMinimumScopePoint)
            {
                var allowedVersion = allowedMinimumScopePoint.ActualVersion;
                return ActualVersion >= allowedVersion;
            }
            
            if (allowed is MustBeInProjectWithMaximumUnityVersion allowedMaximumScopePoint)
            {
                var allowedVersion = allowedMaximumScopePoint.ActualVersion;
                return ActualVersion <= allowedVersion;
            }

            if (!base.IsSubsetOf(allowed))
            {
                return false;
            }
            
            return true;
        }

        public override IEnumerable<Pair<string, string>> EnumerateCustomProperties()
        {
            yield return new Pair<string, string>(VersionProperty, ActualVersion.ToString());
        }

        public override string ToString()
        {
            return $"Unity current version: {ActualVersion}";
        }
    }
}