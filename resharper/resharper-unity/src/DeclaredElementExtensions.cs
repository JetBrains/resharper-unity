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

            return unityEventFunction.TypeName + "." + element.ShortName;
        }
    }
}