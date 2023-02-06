using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    [ShellComponent]
    public class UnityDotsScopeProvider : ScopeProvider
    {
        public UnityDotsScopeProvider()
        {
            Creators.Add(TryToCreate<UnityDotsScope>);
        }

        public override IEnumerable<ITemplateScopePoint> ProvideScopePoints(TemplateAcceptanceContext context)
        {
            if (!context.Solution.HasUnityReference())
                yield break;

            var project = context.GetProject();
            if (project != null && !project.IsUnityProject())
                yield break;

            var packageManager = context.Solution.GetComponent<PackageManager>();
            if (packageManager.HasPackage("com.unity.entities"))
                yield return new UnityDotsScope();
        }
    }
}