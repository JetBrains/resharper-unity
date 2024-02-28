using System;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeAnnotations;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class UnityMinAttributeInformationProvider : IUnityRangeAttributeProvider
{
    public bool IsApplicable(IAttributeInstance attributeInstance)
    {
        if (!attributeInstance.GetClrName().Equals(KnownTypes.MinAttribute))
            return false;
        
        var unityMinValue = attributeInstance.PositionParameter(0);

        if (!unityMinValue.ConstantValue.IsFloat())
            return false;

        return true;
    }

    // Even though the constructor for ValueRange takes long, it only works with int.MaxValue
    public long GetMinValue(IAttributeInstance attributeInstance)
    {
        var unityMinValue = attributeInstance.PositionParameter(0);
        return Convert.ToInt64(Math.Floor(unityMinValue.ConstantValue.FloatValue));
    }

    public long GetMaxValue(IAttributeInstance attributeInstance)
    {
        return long.MaxValue;
    }
}