using System;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeAnnotations;
using JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.CodeAnnotations;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class OdinMaxAttributeInformationProvider : IUnityRangeAttributeProvider
{
    public bool IsApplicable(IAttributeInstance attributeInstance)
    {
        if (!attributeInstance.GetClrName().Equals(OdinKnownAttributes.MaxValueAttribute))
            return false;
        
        var unityMinValue = attributeInstance.PositionParameter(0);

        if (!OdinCodeAnnotationUtil.IsApplicable(unityMinValue.ConstantValue))
            return false;

        return true;
    }

    // Even though the constructor for ValueRange takes long, it only works with int.MaxValue
    public long GetMinValue(IAttributeInstance attributeInstance)
    {
        return long.MinValue;
    }

    public long GetMaxValue(IAttributeInstance attributeInstance)
    {
        var unityMaxValue = attributeInstance.PositionParameter(0);
        return OdinCodeAnnotationUtil.GetMaxValue(unityMaxValue.ConstantValue);
    }
}