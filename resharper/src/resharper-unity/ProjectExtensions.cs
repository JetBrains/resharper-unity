using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Utils;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Properties.Flavours;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using JetBrains.Util.Reflection;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class ProjectExtensions
    {
        public const string AssetsFolder = "Assets";
        public const string ProjectSettingsFolder = "ProjectSettings";
        public const string LibraryFolder = "Library";

        private static readonly AssemblyNameInfo ourUnityEngineReferenceName = AssemblyNameInfoFactory.Create2("UnityEngine", null);
        private static readonly AssemblyNameInfo ourUnityEditorReferenceName = AssemblyNameInfoFactory.Create2("UnityEditor", null);

        // Unity 2017.3 has refactored UnityEngine into modules. Generated projects will still
        // reference UnityEngine.dll as well as the new module assemblies. But non-generated
        // projects (with output copied to Assets) can now reference the modules. Transitive
        // dependencies mean that these projects will reference either UnityEngine.CoreModule or
        // UnityEngine.SharedInternalsModule. Best practice is still to reference UnityEngine.dll,
        // but we need to cater for all sorts of projects.
        private static readonly AssemblyNameInfo ourUnityEngineCoreModuleReferenceName = AssemblyNameInfoFactory.Create2("UnityEngine.CoreModule", null);
        private static readonly AssemblyNameInfo ourUnityEngineSharedInternalsModuleReferenceName = AssemblyNameInfoFactory.Create2("UnityEngine.SharedInternalsModule", null);

        public static readonly ICollection<AssemblyNameInfo> UnityReferenceNames = new List<AssemblyNameInfo>()
        {
            ourUnityEditorReferenceName, ourUnityEngineReferenceName, ourUnityEngineCoreModuleReferenceName, ourUnityEngineSharedInternalsModuleReferenceName
        };

        public static bool HasUnityReference([NotNull] this ISolution solution)
        {
            var tracker = solution.GetComponent<UnityReferencesTracker>();
            return tracker.HasUnityReference.Value;
        }

        public static bool IsUnityProject([CanBeNull] this IProject project)
        {
            // Only VSTU adds the Unity project flavour. Unity + Rider don't, so we have to look at references
            return project != null && (project.HasFlavour<UnityProjectFlavor>() || ReferencesUnity(project));
        }

        public static bool IsUnityGeneratedProject([CanBeNull] this IProject project)
        {
            return project != null && project.HasSubItems(AssetsFolder) && IsUnityProject(project);
        }
        
        private static bool ReferencesUnity(IProject project)
        {
            var targetFrameworkId = project.GetCurrentTargetFrameworkId();
            return ReferencesAssembly(project, targetFrameworkId, ourUnityEngineReferenceName)
                   || ReferencesAssembly(project, targetFrameworkId, ourUnityEditorReferenceName)
                   || ReferencesAssembly(project, targetFrameworkId, ourUnityEngineCoreModuleReferenceName)
                   || ReferencesAssembly(project, targetFrameworkId, ourUnityEngineSharedInternalsModuleReferenceName);
        }

        private static bool ReferencesAssembly(IProject project, TargetFrameworkId targetFrameworkId, AssemblyNameInfo name)
        {            
            return ReferencedAssembliesService.IsProjectReferencingAssemblyByName(project,
                targetFrameworkId, name, out _);
        }
    }
}