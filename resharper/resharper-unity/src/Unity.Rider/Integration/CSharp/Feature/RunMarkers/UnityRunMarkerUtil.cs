using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.CSharp.Feature.RunMarkers
{
    public static class UnityRunMarkerUtil
    {
        [Pure]
        public static bool IsSuitableStaticMethod([NotNull] IMethod method)
        {
            if (!method.IsStatic || method.TypeParameters.Count != 0) return false;
            if (!method.Parameters.IsEmpty()) return false;
            if (method.GetAttributeInstances(false).All(a => a.GetClrName().FullName != "UnityEditor.MenuItem")) return false;
            var parentClass = method.GetContainingType();
            if (parentClass == null) return false;
            var parentClassOwner = parentClass.GetContainingType();
            return parentClassOwner == null || !parentClassOwner.IsClassLike();
        }
    }
}