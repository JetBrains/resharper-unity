using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
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
            if (!context.GetProject().IsUnityProject())
                yield break;

            if(context.Location == null)
                yield break;
            
            var packageManager = context.Solution.GetComponent<PackageManager>();
            if (packageManager.Packages.Any(p => p.Key.Contains("com.unity.entities")))
                yield return new UnityDotsScope();
        }
    }
}