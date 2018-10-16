using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates
{
    // Provides the scope points that are valid for the given context
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
            if (!context.Solution.HasUnityReference())
                yield break;

            var project = context.GetProject();
            if (project != null && !project.IsUnityProject())
                yield break;

            // We could check for C# here, like InRazorCSharpProject, but we only really support C# Unity projects
            // Are there any other types?
            yield return new InUnityCSharpProject();

            var folders = GetFoldersFromProjectFolder(context) ?? GetFoldersFromPath(context);
            if (folders == null || folders.IsEmpty())
                yield break;

            var rootFolder = folders[folders.Count - 1];
            if (rootFolder.Equals(ProjectExtensions.AssetsFolder, StringComparison.OrdinalIgnoreCase))
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

        [CanBeNull]
        private List<string> GetFoldersFromProjectFolder(TemplateAcceptanceContext context)
        {
            var projectFolder = context.GetProjectFolder();
            if (projectFolder == null)
                return null;

            var folders = new List<string>();
            while (projectFolder?.Path?.ShortName != null)
            {
                folders.Add(projectFolder.Path.ShortName);
                projectFolder = projectFolder.ParentFolder;
            }
            return folders;
        }

        [CanBeNull]
        private List<string> GetFoldersFromPath(TemplateAcceptanceContext context)
        {
            if (context.Location == null)
                return null;

            var folders = new List<string>();
            var currentPath = context.Location;
            while (!currentPath.IsEmpty)
            {
                var folder = currentPath.Name;
                folders.Add(folder);
                if (folder == ProjectExtensions.AssetsFolder)
                    break;
                currentPath = currentPath.Parent;
            }
            return folders;
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