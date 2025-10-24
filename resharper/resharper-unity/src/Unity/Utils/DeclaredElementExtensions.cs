using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    public static class DeclaredElementExtensions
    {
        public static bool IsFromUnityProject([NotNull] this IDeclaredElement element)
        {
            // GetSourceFiles may be heavy for C++ IDeclaredElement
            return element.PresentationLanguage.Is<CSharpLanguage>() && element.GetSourceFiles().Any(sf => sf.GetProject().IsUnityProject());
        }

        public static bool IsBuiltInUnityClass(this IDeclaredElement element)
        {
            if (element is CompiledTypeElement compiledTypeElement &&
                compiledTypeElement.Module is IAssemblyPsiModule assemblyPsiModule)
            {
                var name = assemblyPsiModule.Assembly.AssemblyName.Name;
                if (name.StartsWith("UnityEngine") || name.StartsWith("UnityEditor"))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool DerivesFrom([CanBeNull] this ITypeElement candidate, IClrTypeName baseTypeName)
        {
            if (candidate == null)
                return false;
            var knownTypesCache = candidate.GetSolution().GetComponent<KnownTypesCache>();
            var baseType = GetTypeElement(baseTypeName, knownTypesCache, candidate.Module);
            return candidate.IsDescendantOf(baseType);
        }

        private static ITypeElement GetTypeElement(IClrTypeName typeName, KnownTypesCache knownTypesCache,
                                                   IPsiModule module)
        {
            using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
            {
                var type = knownTypesCache.GetByClrTypeName(typeName, module);
                return type.GetTypeElement();
            }
        }

        public static bool DerivesFromMonoBehaviour([CanBeNull] this ITypeElement candidate)
        {
            return candidate.DerivesFrom(KnownTypes.MonoBehaviour);
        }
        
        public static bool DerivesFromOdinDrawer([CanBeNull] this ITypeElement candidate)
        {
            return candidate.DerivesFrom(OdinKnownAttributes.OdinDrawer);
        }
        
        public static bool DerivesFromOdinAttributeProcessor([CanBeNull] this ITypeElement candidate)
        {
            return candidate.DerivesFrom(OdinKnownAttributes.OdinAttributeProcessor);
        }

        public static bool DerivesFromScriptableObject([CanBeNull] this ITypeElement candidate)
        {
            return candidate.DerivesFrom(KnownTypes.ScriptableObject);
        }

        public static bool DerivesFromUnityEvent([CanBeNull] this ITypeElement candidate)
        {
            return candidate.DerivesFrom(KnownTypes.UnityEvent);
        }

        [CanBeNull]
        public static string GetUnityEventFunctionName(this IDeclaredElement element, UnityApi unityApi)
        {
            var method = element as IMethod;
            if (method == null && element is IParameter parameter)
                method = parameter.ContainingParametersOwner as IMethod;

            if (method == null)
                return null;

            var unityEventFunction = unityApi.GetUnityEventFunction(method);
            if (unityEventFunction == null)
                return null;

            return unityEventFunction.TypeName.GetFullNameFast() + "." + method.ShortName; // RIDER-60297
        }

        public static bool IsUnityComponent(this ITypeElement typeElement, out bool isBuiltin)
        {
            // User components must derive from MonoBehaviour, but built in components only have to derive from
            // Component. A built in component will be something that isn't an asset, which means it's come from
            // one of the UnityEngine assemblies, or UnityEditor.dll. Another check might be that the referenced
            // module lives in Assets, but packages makes a mess of that (referenced packages are compiled and
            // referenced from Library or local packages can include a dll as an asset, external to the project)
            isBuiltin = typeElement.IsBuiltInUnityClass();
            return isBuiltin ? typeElement.DerivesFrom(KnownTypes.Component) : typeElement.DerivesFromMonoBehaviour();
        }

        public static bool IsClrName(this ITypeElement typeElement, IClrTypeName clrTypeName)
        {
            return typeElement?.GetClrName().Equals(clrTypeName) ?? false;
        }
    }
}