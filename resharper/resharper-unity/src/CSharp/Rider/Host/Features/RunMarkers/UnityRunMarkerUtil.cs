using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Rider.Host.Features.RunMarkers
{
  public static class UnityRunMarkerUtil
  {
    [Pure]
    public static bool IsSuitableStaticMethod([NotNull] IMethod method)
    {
      if (!method.IsStatic || method.TypeParameters.Count != 0) return false;
      if (!method.Parameters.IsEmpty()) return false;
      if (method.GetAttributeInstances(false).IsEmpty()) return false;
      if (!method.ReturnType.IsVoid()) return false;
      var parentClass = method.GetContainingType();
      if (parentClass == null ) return false;
      var parentClassOwner = parentClass.GetContainingType();
      return parentClassOwner == null || !parentClassOwner.IsClassLike();
    }
  }
}