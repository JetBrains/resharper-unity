using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Utils;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Properties.Flavours;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Reflection;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class UnityReferenceDetectionExtensions
    {
        private static readonly string[] ourUnitySimpleAssemblyNames =
        {
            "UnityEngine",
            "UnityEditor",

            // Unity 2017.3 has refactored UnityEngine into modules. Generated projects will still
            // reference UnityEngine.dll as well as the new module assemblies. But non-generated
            // projects (with output copied to Assets) can now reference the modules. Transitive
            // dependencies mean that these projects will reference either UnityEngine.CoreModule or
            // UnityEngine.SharedInternalsModule. Best practice is still to reference UnityEngine.dll,
            // but we need to cater for all sorts of projects.
            "UnityEngine.CoreModule",
            "UnityEngine.SharedInternalsModule"
        };

        public static readonly AssemblyNameInfo[] UnityReferenceNames = ourUnitySimpleAssemblyNames.Select(n => AssemblyNameInfoFactory.Create2(n, null)).ToArray();
        
        public static bool IsFromUnityProject(this ITreeNode treeNode)
        {
            var psiModule = treeNode.GetPsiModule();
            var psiServices = treeNode.GetPsiServices();
            return psiModule.IsReferencingUnityModule(psiServices);
        }
        
        public static bool IsFromUnityProject(this IDeclaredElement element)
        {
            if (!(element is IClrDeclaredElement clrDeclaredElement))
                return false;

            var psiModule = clrDeclaredElement.Module;
            var psiServices = clrDeclaredElement.GetPsiServices();
            return psiModule.IsReferencingUnityModule(psiServices);
        }

        private static bool IsReferencingUnityModule([NotNull] this IPsiModule psiModule, IPsiServices psiServices)
        {
            if (!psiModule.IsValid()) // TODO: validity check can be removed after the RIDER-11332 is fixed properly
                return false;

            return psiServices
                .Modules
                .GetModuleReferences(psiModule)
                .Any(r => r.Module.IsUnityModule());
        }
        
        private static bool IsUnityModule([NotNull] this IPsiModule psiModule)
        {   
            return ourUnitySimpleAssemblyNames.Contains(psiModule.Name);
        }

        public static bool IsUnityProject([CanBeNull] this IProject project)
        {
            // Only VSTU adds the Unity project flavour. Unity + Rider don't, so we have to look at references
            return project != null && (project.HasFlavour<UnityProjectFlavor>() || ReferencesUnity(project));
        }

        public static bool IsProjectCompiledByUnity([CanBeNull] this IProject project)
        {
            return project != null && project.HasSubItems("Assets") && IsUnityProject(project);
        }

        private static bool ReferencesUnity(IProject project)
        {
            var targetFrameworkId = project.GetCurrentTargetFrameworkId();
            return UnityReferenceNames.Any(ani => ReferencesAssembly(project, targetFrameworkId, ani));
        }

        private static bool ReferencesAssembly(IProject project, TargetFrameworkId targetFrameworkId, AssemblyNameInfo name)
        {            
            return ReferencedAssembliesService.IsProjectReferencingAssemblyByName(project, targetFrameworkId, name, out var _);
        }
    }
}