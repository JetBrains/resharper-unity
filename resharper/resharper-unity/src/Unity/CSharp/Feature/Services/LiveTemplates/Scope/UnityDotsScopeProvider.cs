using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
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

            // Project might be null if the selected file or folder belongs to more than one project. In this case, we
            // should get a valid Location, which will be the folder of the selected file
            var project = context.GetProject();
            if (project != null && !project.IsUnityProject())
                yield break;

            var packageManager = context.Solution.GetComponent<PackageManager>();
            if (packageManager.HasPackage(PackageManager.UnityEntitiesPackageName))
                yield return new UnityDotsScope();
        }
    }
}