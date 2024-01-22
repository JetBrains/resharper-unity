using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeAnnotations;
using JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.CodeAnnotations;

[SolutionComponent]
public class OdinMinAttributeInformationProvider : IUnityRangeAttributeProvider
{
    public bool IsApplicable(IAttributeInstance attributeInstance)
    {
        if (!attributeInstance.GetClrName().Equals(OdinKnownAttributes.MinValueAttribute))
            return false;
        
        var unityMinValue = attributeInstance.PositionParameter(0);
        var unityMaxValue = attributeInstance.PositionParameter(1);

        if (!OdinCodeAnnotationUtil.IsApplicable(unityMinValue.ConstantValue) && !OdinCodeAnnotationUtil.IsApplicable(unityMaxValue.ConstantValue))
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
        var unityMaxValue = attributeInstance.PositionParameter(0);
        return OdinCodeAnnotationUtil.GetMaxValue(unityMaxValue.ConstantValue);
    }
}