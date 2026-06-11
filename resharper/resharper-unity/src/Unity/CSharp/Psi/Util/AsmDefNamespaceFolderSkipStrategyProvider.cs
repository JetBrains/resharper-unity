#nullable enable
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Util
{
    /// <summary>
    /// Namespace folder skip strategy that skips folders up from the Unity assembly definition (.asmdef) location.
    /// </summary>
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class AsmDefNamespaceFolderSkipStrategyProvider(AsmDefCache asmDefCache) : INamespaceFolderSkipStrategyProvider
    {
        public INamespaceFolderSkipStrategy? GetNamespaceFolderSkipStrategy(IProjectItem projectItem, PsiLanguageType language)
        {
            if (!language.Is<CSharpLanguage>()) return null;

            var project = projectItem.GetProject();
            if (project == null || !project.IsUnityProject()) return null;

            var settingsStore = project.GetSolution().GetSettingsStore();
            var featureEnabled = settingsStore.GetValue((UnitySettings s) => s.UseAsmDefFolderAsNamespaceRoot);
            if (!featureEnabled) return null;
            
            var asmName = project.Name; // generated projects are named after assembly names, so they are interchangeable
            var asmDefLocation = asmDefCache.GetAsmDefLocationByAssemblyName(asmName);
            if (asmDefLocation.IsEmpty) return null;

            // rootNamespace from asmdef ends up as DefaultNamespace in the generated csproj, so use that to determine whether rootNamespace is defined
            // NOTE: this requires unity to re-generate project files after changing the rootNamespace in an asmdef
            var hasRootNamespace = project.ProjectProperties.BuildSettings is IManagedProjectBuildSettings buildSettings
                                   && buildSettings.DefaultNamespace.IsNotEmpty();

            return new Strategy(asmDefLocation.Parent, hasRootNamespace);
        }

        private class Strategy(VirtualFileSystemPath asmDefLocation, bool hasRootNamespace) : INamespaceFolderSkipStrategy
        {
            public bool ShouldSkipFolder(IProjectFolder folder)
            {
                if (folder.Location == asmDefLocation)
                {
                    // for the asmdef folder itself, if the assembly defines a root namespace, we definitely skip the folder name (since the root namespace will be used instead)
                    // otherwise let other strategies (i.e. "namespace provider" value) handle whether the folder should contribute
                    return hasRootNamespace;
                } 
                
                // ...but always skip everything above the asmdef folder
                return folder.Location.IsPrefixOf(asmDefLocation);
            }
        }
    }
}