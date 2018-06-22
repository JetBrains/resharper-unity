using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.LiveTemplates.Scope
{
    [ShellComponent]
    public class UnityProjectVersionProvider : IScopeProvider
    {
        public IEnumerable<ITemplateScopePoint> ProvideScopePoints(TemplateAcceptanceContext context)
        {
            if (!context.Solution.IsUnitySolution())
                yield break;

            var project = context.GetProject();
            if (project != null && !project.IsUnityProject())
                yield break;

            var version = context.Solution.GetComponent<UnityVersion>().GetActualVersion(project);
            if (version.Major != 0)
                yield return new InProjectWithUnityVersion(version);
        }

        public ITemplateScopePoint ReadFromXml(XmlElement scopeElement)
        {
            return scopeElement.GetAttribute(TemplateScopePoint.AttrType) != InProjectWithUnityVersion.TypeName
                ? null
                : new InProjectWithUnityVersion(Version.Parse(scopeElement.GetAttribute(InProjectWithUnityVersion.VersionProperty)));
        }

        public ITemplateScopePoint CreateScope(Guid scopeGuid, string typeName,
            IEnumerable<Pair<string, string>> customProperties)
        {
            if (typeName != InProjectWithUnityVersion.TypeName)
                return null;

            var versionString = customProperties.Where(p => p.First == InProjectWithUnityVersion.VersionProperty).Select(p => p.Second)
                .FirstOrDefault();
            if (versionString == null)
                return null;

            var version = Version.Parse(versionString);
            return new InProjectWithUnityVersion(version) {UID = scopeGuid};
        }
    }
}