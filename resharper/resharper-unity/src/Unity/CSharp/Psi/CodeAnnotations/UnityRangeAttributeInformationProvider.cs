using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeAnnotations;

[SolutionComponent]
public class UnityRangeAttributeInformationProvider : IUnityRangeAttributeProvider
{
    public bool IsApplicable(IAttributeInstance attributeInstance)
    {
        if (!attributeInstance.GetClrName().Equals(KnownTypes.RangeAttribute))
            return false;
        
        var unityFrom = attributeInstance.PositionParameter(0);
        var unityTo = attributeInstance.PositionParameter(1);

        // Values are floats, but applied to an integer field. Convert to integer values
        if (!unityFrom.ConstantValue.IsFloat() || !unityTo.ConstantValue.IsFloat())
            return false;

        return true;
    }

    // The check above means this is not null. We take the floor, because that's how Unity works.
    // E.g. Unity's Inspector treats [Range(1.7f, 10.9f)] as between 1 and 10 inclusive
    
    public long GetMinValue(IAttributeInstance attributeInstance)
    {
        var unityFrom = attributeInstance.PositionParameter(0);
        return Convert.ToInt64(Math.Floor(unityFrom.ConstantValue.FloatValue));
    }

    public long GetMaxValue(IAttributeInstance attributeInstance)
    {
        var unityTo = attributeInstance.PositionParameter(1);
        return Convert.ToInt64(Math.Floor(unityTo.ConstantValue.FloatValue));
    }
}