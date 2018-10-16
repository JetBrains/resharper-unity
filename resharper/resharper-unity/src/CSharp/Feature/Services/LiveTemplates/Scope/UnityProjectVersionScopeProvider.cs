using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
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
            if (project != null && !project.IsUnityProject())
                yield break;

            var version = context.Solution.GetComponent<UnityVersion>().GetActualVersion(project);
            if (version.Major != 0)
                yield return new MustBeInProjectWithUnityVersion(version);
        }

        public ITemplateScopePoint ReadFromXml(XmlElement scopeElement)
        {
            return scopeElement.GetAttribute(TemplateScopePoint.AttrType) != MustBeInProjectWithUnityVersion.TypeName
                ? null
                : new MustBeInProjectWithUnityVersion(Version.Parse(scopeElement.GetAttribute(MustBeInProjectWithUnityVersion.VersionProperty)));
        }

        public ITemplateScopePoint CreateScope(Guid scopeGuid, string typeName,
            IEnumerable<Pair<string, string>> customProperties)
        {
            if (typeName != MustBeInProjectWithUnityVersion.TypeName)
                return null;

            var versionString = customProperties.Where(p => p.First == MustBeInProjectWithUnityVersion.VersionProperty).Select(p => p.Second)
                .FirstOrDefault();
            if (versionString == null)
                return null;

            var version = Version.Parse(versionString);
            return new MustBeInProjectWithUnityVersion(version) {UID = scopeGuid};
        }
    }
}