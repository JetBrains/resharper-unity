using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityDotsScopeProvider : ScopeProvider
    {
        public UnityDotsScopeProvider()
        {
            Creators.Add(TryToCreate<UnityDotsScope>);
        }

        public override IEnumerable<ITemplateScopePoint> ProvideScopePoints(TemplateAcceptanceContext context)
        {
            if(DotsUtils.IsUnityProjectWithEntitiesPackage(context))
                yield return new UnityDotsScope();
        }
    }
}