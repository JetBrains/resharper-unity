using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class DeclaredElementExtensions
    {
        public static bool IsFromUnityProject(this IDeclaredElement element)
        {
            return element.GetSourceFiles().Any(sf => sf.GetProject().IsUnityProject());
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

        public static bool DerivesFrom(this IDeclaredElement candidate, IClrTypeName baseTypeName)
        {
            if (!(candidate is ITypeElement element))
                return false;
            var baseType = GetTypeElement(baseTypeName, element.Module);
            return element.IsDescendantOf(baseType);
        }

        private static ITypeElement GetTypeElement(IClrTypeName typeName, IPsiModule module)
        {
            using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
            {
                var type = TypeFactory.CreateTypeByCLRName(typeName, module);
                return type.GetTypeElement();
            }
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

            return unityEventFunction.TypeName.GetFullNameFast() + "." + element.ShortName;
        }
        
        public static bool IsUnityComponent(this ITypeElement typeElement, out bool IsBuiltin)
        {
            // User components must derive from MonoBehaviour, but built in components only have to derive from
            // Component. A built in component will be something that isn't an asset, which means it's come from
            // one of the UnityEngine assemblies, or UnityEditor.dll. Another check might be that the referenced
            // module lives in Assets, but packages makes a mess of that (referenced packages are compiled and
            // referenced from Library or local packages can include a dll as an asset, external to the project)
            IsBuiltin = typeElement.IsBuiltInUnityClass();
            if (IsBuiltin)
                return typeElement.DerivesFrom(KnownTypes.Component);

            return typeElement.DerivesFrom(KnownTypes.MonoBehaviour);
        }
    }
}