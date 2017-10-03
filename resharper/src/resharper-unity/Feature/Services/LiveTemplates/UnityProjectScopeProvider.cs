using System;
using System.Collections.Generic;
using System.Linq;
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
            Creators.Add(TryToCreate<InUnityCSharpAssetsFolder>);
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

            var projectFolder = context.GetProjectFolder();
            if (projectFolder != null)
            {
                var folders = new List<string>();
                while (projectFolder?.Path?.ShortName != null)
                {
                    folders.Add(projectFolder.Path.ShortName);
                    projectFolder = projectFolder.ParentFolder;
                }

                if (folders.Any(f => f.Equals("Assets", StringComparison.OrdinalIgnoreCase)))
                    yield return new InUnityCSharpAssetsFolder();
            }
        }
    }
}