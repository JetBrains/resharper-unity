using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class DeclaredElementExtensions
    {
        public static bool IsFromUnityProject(this IDeclaredElement element)
        {
            return element.GetSourceFiles().Any(sf => sf.GetProject().IsUnityProject());
        }
    }
}