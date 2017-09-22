using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.LiveTemplates.Scope;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.LiveTemplates
{
    // Provides the sope points that are valid for the given context
    [ShellComponent]
    public class UnityProjectScopeProvider : ScopeProvider
    {
        public UnityProjectScopeProvider()
        {
            // Used when creating scope point from settings
            Creators.Add(TryToCreate<InUnityCSharpProject>);
        }

        public override IEnumerable<ITemplateScopePoint> ProvideScopePoints(TemplateAcceptanceContext context)
        {
            var project = context.GetProject();
            if (project == null)
                yield break;

            if (!project.IsUnityProject())
                yield break;

            // We could check for C# here, like InRazorCSharpProject, but we only really support C# Unity projects
            // Are there any other types?
            yield return new InUnityCSharpProject();

            // TODO: Maybe add extra scopes? E.g. Plugins, Editor, Assets folders?
            // See context.GetProjectFolder to get the folder for the context
        }
    }
}