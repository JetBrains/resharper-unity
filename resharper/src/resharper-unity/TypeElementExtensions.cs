using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class TypeElementExtensions
    {
        public static bool IsDescendantOf(this ITypeElement typeElement, IClrTypeName baseClass)
        {
            if (typeElement.GetClrName().Equals(baseClass))
                return true;

            foreach (var superClass in typeElement.GetAllSuperClasses())
            {
                if (superClass.GetClrName().Equals(baseClass))
                    return true;
            }
            return false;
        }
    }
}
