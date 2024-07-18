using System;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeAnnotations;
using JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.CodeAnnotations;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class OdinRangeAttributesInformationProvider : IUnityRangeAttributeProvider
{
    public bool IsApplicable(IAttributeInstance attributeInstance)
    {
        if (!attributeInstance.GetClrName().Equals(OdinKnownAttributes.MinMaxSliderAttribute) 
            && !attributeInstance.GetClrName().Equals(OdinKnownAttributes.ProgressBarAttribute)
            && !attributeInstance.GetClrName().Equals(OdinKnownAttributes.PropertyRangeAttribute)
            && !attributeInstance.GetClrName().Equals(OdinKnownAttributes.WrapAttribute))
            return false;
        
        var unityMinValue = attributeInstance.PositionParameter(0);
        var unityMaxValue = attributeInstance.PositionParameter(1);

        if (!OdinCodeAnnotationUtil.IsApplicable(unityMinValue.ConstantValue))
            return false;
        
        if (!OdinCodeAnnotationUtil.IsApplicable(unityMaxValue.ConstantValue))
            return false;

        return true;
    }

    // Even though the constructor for ValueRange takes long, it only works with int.MaxValue
    public long GetMinValue(IAttributeInstance attributeInstance)
    {
        var unityMinValue = attributeInstance.PositionParameter(0);
        return OdinCodeAnnotationUtil.GetMinValue(unityMinValue.ConstantValue);
    }

    public long GetMaxValue(IAttributeInstance attributeInstance)
    {
        var unityMinValue = attributeInstance.PositionParameter(1);
        return OdinCodeAnnotationUtil.GetMaxValue(unityMinValue.ConstantValue);
    }
}