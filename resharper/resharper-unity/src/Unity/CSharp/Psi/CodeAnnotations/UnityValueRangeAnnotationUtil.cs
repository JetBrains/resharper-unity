using System;
using System.Diagnostics.Contracts;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeAnnotations;

public static class UnityValueRangeAnnotationUtil
{
    [Pure]
    public static bool IsApplicable(IAttributesOwner attributesOwner, UnityApi unityApi)
    {
        if (attributesOwner is not IField field)
            return false;

        if (!attributesOwner.IsFromUnityProject())
            return false;

        if (unityApi.IsSerialisedField(field).HasFlag(SerializedFieldStatus.NonSerializedField))
            return false;

        return true;
    }

    [Pure]
    public static long ConvertToLong(ConstantValue floatValue)
    {
        return Convert.ToInt64(Math.Floor(floatValue.FloatValue));
    }
}