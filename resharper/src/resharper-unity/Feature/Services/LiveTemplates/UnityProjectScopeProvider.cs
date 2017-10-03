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
            Creators.Add(TryToCreate<InUnityCSharpEditorFolder>);
            Creators.Add(TryToCreate<InUnityCSharpRuntimeFolder>);
            Creators.Add(TryToCreate<InUnityCSharpFirstpassFolder>);
            Creators.Add(TryToCreate<InUnityCSharpFirstpassEditorFolder>);
            Creators.Add(TryToCreate<InUnityCSharpFirstpassRuntimeFolder>);
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

                if (folders.Count > 0)
                {
                    var rootFolder = folders[folders.Count - 1];
                    if (rootFolder.Equals("Assets", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return new InUnityCSharpAssetsFolder();

                        var isFirstpass = IsFirstpass(folders);
                        var isEditor = folders.Any(f => f.Equals("Editor", StringComparison.OrdinalIgnoreCase));

                        if (isFirstpass)
                        {
                            yield return new InUnityCSharpFirstpassFolder();
                            if (isEditor)
                                yield return new InUnityCSharpFirstpassEditorFolder();
                            if (!isEditor)
                                yield return new InUnityCSharpFirstpassRuntimeFolder();
                        }
                        else
                        {
                            if (isEditor)
                                yield return new InUnityCSharpEditorFolder();
                            if (!isEditor)
                                yield return new InUnityCSharpRuntimeFolder();
                        }
                    }
                }
            }
        }

        private bool IsFirstpass(List<string> folders)
        {
            if (folders.Count > 1)
            {
                // We already know that folders[folders.Count - 1] == "Assets"
                var toplevelFolder = folders[folders.Count - 2];
                return toplevelFolder.Equals("Standard Assets", StringComparison.OrdinalIgnoreCase)
                       || toplevelFolder.Equals("Pro Standard Assets", StringComparison.OrdinalIgnoreCase)
                       || toplevelFolder.Equals("Plugins", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}