using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    [ShellComponent]
    public class UnityProjectVersionScopeProvider : IScopeProvider
    {
        public IEnumerable<ITemplateScopePoint> ProvideScopePoints(TemplateAcceptanceContext context)
        {
            if (!context.Solution.HasUnityReference())
                yield break;

            var project = context.GetProject();
            var version = project != null
                ? context.Solution.GetComponent<UnityVersion>().GetActualVersion(project)
                : context.Solution.GetComponent<UnityVersion>().ActualVersionForSolution.Value;

            if (version.Major != 0)
                yield return new MustBeInProjectWithCurrentUnityVersion(version);
        }

        public ITemplateScopePoint CreateScope(Guid scopeGuid, string typeName, IEnumerable<Pair<string, string>> customProperties)
        {
            if (typeName == MustBeInProjectWithMinimumUnityVersion.TypeName)
            {
                var versionString = customProperties.Where(p => p.First == MustBeInProjectWithMinimumUnityVersion.VersionProperty)
                    .Select(p => p.Second)
                    .FirstOrDefault();
                if (versionString == null)
                    return null;

                var version = Version.Parse(versionString);
                return new MustBeInProjectWithMinimumUnityVersion(version) {UID = scopeGuid};
            }

            if (typeName == MustBeInProjectWithMaximumUnityVersion.TypeName)
            {
                var versionString = customProperties.Where(p => p.First == MustBeInProjectWithMaximumUnityVersion.VersionProperty)
                    .Select(p => p.Second)
                    .FirstOrDefault();
                if (versionString == null)
                    return null;

                var version = Version.Parse(versionString);
                return new MustBeInProjectWithMaximumUnityVersion(version) {UID = scopeGuid};
            }

            return null;
        }
    }
}