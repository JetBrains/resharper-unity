using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Util
{
    [SolutionComponent]
    public class ExternalPackageCustomNamespaceProvider : ICustomDefaultNamespaceProvider
    {
        private static readonly Key<DefaultNamespace?> ourCalculatedDefaultNamespaceKey = new("Unity::ExternalPackage::DefaultNamespace");

        private readonly NamespaceFolderProperty myNamespaceFolderProperty;

        public ExternalPackageCustomNamespaceProvider(NamespaceFolderProperty namespaceFolderProperty)
        {
            myNamespaceFolderProperty = namespaceFolderProperty;
        }

        public ExpectedNamespaceAndNamespaceChecker CalculateCustomNamespace(IProjectItem projectItem, PsiLanguageType language)
        {
            var project = projectItem.GetProject().NotNull();
            if (!ShouldProvideCustomNamespace(project, projectItem, language))
                return new ExpectedNamespaceAndNamespaceChecker(null);

            return new ExpectedNamespaceAndNamespaceChecker(CalculateNamespace(projectItem,
                language.LanguageService().NotNull("languageService != null")));
        }

        private static bool ShouldProvideCustomNamespace(IProject project, IProjectItem projectItem, PsiLanguageType language)
        {
            if (!project.IsUnityProject() || !language.Is<CSharpLanguage>())
                return false;

            // If the project item lives under the solution location, we don't need to do anything. The default
            // namespace handling, coupled with our namespace provider settings in NamespaceProviderProjectSettingsProvider
            // means everything works
            if (project.Location.IsPrefixOf(projectItem.Location))
                return false;

            // If the project has a default namespace, do nothing. When the project is based on an .asmdef file, the
            // linked root folder is the location of the .asmdef file, and the namespaces we want are relative to the
            // .asmdef file's default namespace
            var buildSettings = project.ProjectProperties.BuildSettings as IManagedProjectBuildSettings;
            var defaultNamespace = buildSettings?.DefaultNamespace;
            if (!defaultNamespace.IsNullOrEmpty())
                return false;

            // We're only interested in external packages, which we can identify by checking that the immediate child of
            // the project is a linked item (it links to an item external to the solution)
            var rootFolder = GetRootFolder(projectItem);
            return rootFolder?.IsLinked == true;
        }

        private static IProjectFolder? GetRootFolder(IProjectItem projectItem)
        {
            var projectFolder = projectItem.ParentFolder;
            while (projectFolder != null)
            {
                if (projectFolder.ParentFolder is IProject)
                    return projectFolder;

                projectFolder = projectFolder.ParentFolder;
            }

            return null;
        }

        private string? CalculateNamespace(IProjectItem item, LanguageService languageService)
        {
            if (item.ParentFolder is IProject project && item is IProjectFolder rootFolder)
                return CalculateNamespaceForProjectFromRootFolder(project, rootFolder, languageService);

            var parentFolder = item.ParentFolder;
            if (parentFolder == null) return null;

            var parentNamespace = CalculateNamespace(parentFolder, languageService);
            if (parentNamespace == null)
                return null;

            if (item is IProjectFolder folder)
            {
                var isNamespaceProvider = myNamespaceFolderProperty.GetNamespaceFolderProperty(folder);
                if (!isNamespaceProvider)
                    return parentNamespace;
            }

            if (item is IProjectFile)
                return parentNamespace;

            var suffix = NamespaceFolderUtil.MakeValidQualifiedName(item.Name, languageService);

            if (parentNamespace.Length > 0)
            {
                if (string.IsNullOrEmpty(suffix)) return parentNamespace;
                return $"{parentNamespace}.{suffix}";
            }

            return suffix;
        }

        private string? CalculateNamespaceForProjectFromRootFolder(IProject project, IProjectFolder rootFolder,
                                                                   LanguageService languageService)
        {
            if (!rootFolder.IsLinked || rootFolder.Path == null) return null;

            var isNamespaceProvider = myNamespaceFolderProperty.GetNamespaceFolderProperty(rootFolder);

            // Cache the calculated namespace for the root folder, but make sure it's calculated with the same namespace
            // provider state!
            var calculatedNamespace = project.GetData(ourCalculatedDefaultNamespaceKey);
            if (calculatedNamespace != null && calculatedNamespace.RootFolderIsNamespaceProvider == isNamespaceProvider)
                return calculatedNamespace.RootFolderNamespace;

            var rootFolderNamespace = string.Empty;
            var location = rootFolder.Path.ReferencedFolderPath;
            var packageJsonDirectory = FileSystemUtil.TryGetDirectoryNameOfFileAbove(location, "package.json");
            if (packageJsonDirectory != null)
            {
                var path = location.MakeRelativeTo(packageJsonDirectory);

                // MAKE SURE TO KEEP UP TO DATE WITH THE RULES IN NamespaceProviderProjectSettingsProvider
                if (path.StartsWith("Runtime/Scripts"))
                    path = path.RemovePrefix("Runtime/Scripts");
                else if (path.StartsWith("Scripts/Runtime"))
                    path = path.RemovePrefix("Scripts/Runtime");
                else if (path.StartsWith("Runtime"))
                    path = path.RemovePrefix("Runtime");
                else if (path.StartsWith("Scripts"))
                    path = path.RemovePrefix("Scripts");

                // The user might have excluded the root folder as a namespace provider. If so, skip and reset the flag.
                // This means there is no way to exclude folders between the package.json location and the root folder
                var skipFirstComponent = !isNamespaceProvider;
                foreach (var pathComponent in path.Components)
                {
                    if (skipFirstComponent)
                    {
                        skipFirstComponent = false;
                        continue;
                    }
                    var name = NamespaceFolderUtil.MakeValidQualifiedName(pathComponent.ToString(), languageService);
                    rootFolderNamespace = rootFolderNamespace.IsNullOrEmpty() ? name : $"{rootFolderNamespace}.{name}";
                }
            }

            calculatedNamespace = new DefaultNamespace(rootFolderNamespace, isNamespaceProvider);
            project.PutData(ourCalculatedDefaultNamespaceKey, calculatedNamespace);
            return calculatedNamespace.RootFolderNamespace;
        }

        private class DefaultNamespace
        {
            public readonly string RootFolderNamespace;
            public readonly bool RootFolderIsNamespaceProvider;

            public DefaultNamespace(string rootFolderNamespace, bool rootFolderIsNamespaceProvider)
            {
                RootFolderNamespace = rootFolderNamespace;
                RootFolderIsNamespaceProvider = rootFolderIsNamespaceProvider;
            }
        }
    }
}