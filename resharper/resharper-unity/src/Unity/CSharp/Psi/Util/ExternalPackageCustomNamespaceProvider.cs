using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Util
{
    [SolutionComponent]
    public class ExternalPackageCustomNamespaceProvider : ICustomDefaultNamespaceProvider
    {
        private static readonly Key<string> ourCalculatedDefaultNamespaceKey = new Key<string>("Unity::ExternalPackage::DefaultNamespace");

        public ExpectedNamespaceAndNamespaceChecker CalculateCustomNamespace(IProjectItem projectItem, PsiLanguageType language)
        {
            var project = projectItem.GetProject().NotNull();
            if (!ShouldProvideCustomNamespace(project, projectItem, language))
                return new ExpectedNamespaceAndNamespaceChecker(null);

            var namespaceFolderProperty = project.GetComponent<NamespaceFolderProperty>();
            return new ExpectedNamespaceAndNamespaceChecker(CalculateNamespace(projectItem, language.LanguageService(),
                namespaceFolderProperty));
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

            // Is the root folder a linked folder, pointing externally to the solution?
            var rootFolder = GetRootFolder(projectItem);
            return rootFolder?.IsLinked == true;
        }

        [CanBeNull]
        private static IProjectFolder GetRootFolder(IProjectItem projectItem)
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

        [CanBeNull]
        private static string CalculateNamespace([NotNull] IProjectItem item, LanguageService languageService,
                                                 NamespaceFolderProperty namespaceFolderProperty)
        {
            if (item.ParentFolder is IProject && item is IProjectFolder rootFolder)
                return CalculateNamespaceForProjectFromRootFolder(item.GetProject(), rootFolder, languageService);

            var parentFolder = item.ParentFolder;
            if (parentFolder == null) return null;

            var parentNamespace = CalculateNamespace(parentFolder, languageService, namespaceFolderProperty);
            if (parentNamespace == null)
                return null;

            if (item is IProjectFolder folder)
            {
                var isNamespaceProvider = namespaceFolderProperty.GetNamespaceFolderProperty(folder);
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

        [CanBeNull]
        private static string CalculateNamespaceForProjectFromRootFolder(
            [CanBeNull] IProject project, IProjectFolder rootFolder, LanguageService languageService)
        {
            if (project == null || !rootFolder.IsLinked || rootFolder.Path == null) return null;

            var calculatedNamespace = project.GetData(ourCalculatedDefaultNamespaceKey);
            if (calculatedNamespace != null)
                return calculatedNamespace;

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

                foreach (var pathComponent in path.Components)
                {
                    var name = NamespaceFolderUtil.MakeValidQualifiedName(pathComponent.ToString(), languageService);
                    calculatedNamespace = calculatedNamespace.IsNullOrEmpty() ? name : $"{calculatedNamespace}.{name}";
                }
            }

            project.PutData(ourCalculatedDefaultNamespaceKey, calculatedNamespace);

            return calculatedNamespace;
        }
    }
}