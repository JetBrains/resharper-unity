using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeAnnotations;
using JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.CodeAnnotations;
using JetBrains.ReSharper.Psi.CSharp.Impl.ControlFlow.IntValuesAnalysis;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.CodeAnnotations;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class OdinRangeAttributesInformationProvider(UnityApi unityApi) : ICustomIntValueRangeAnnotationProvider
{
    public IEnumerable<IClrTypeName> AttributeNames => [
        OdinKnownAttributes.MinMaxSliderAttribute,
        OdinKnownAttributes.ProgressBarAttribute,
        OdinKnownAttributes.PropertyRangeAttribute,
        OdinKnownAttributes.WrapAttribute
    ];

    public bool IsApplicable(IAttributesOwner attributesOwner)
    {
        return UnityValueRangeAnnotationUtil.IsApplicable(attributesOwner, unityApi);
    }

    public bool TryApplyAnnotation(IAttributeInstance attributeInstance, AbstractValue.Builder builder)
    {
        if (attributeInstance.PositionParameterCount == 2)
        {
            var firstValue = attributeInstance.PositionParameter(0);
            var secondValue = attributeInstance.PositionParameter(1);

            if (OdinCodeAnnotationUtil.IsApplicable(firstValue.ConstantValue) && OdinCodeAnnotationUtil.IsApplicable(secondValue.ConstantValue))
            {
                var minValue = OdinCodeAnnotationUtil.GetMinValue(firstValue.ConstantValue);
                var maxValue = OdinCodeAnnotationUtil.GetMaxValue(secondValue.ConstantValue);

                if (minValue <= maxValue)
                {
                    builder.Add(new AbstractValue.LongInterval(minValue, maxValue));
                    return true;
                }
            }
        }

        return false;
    }
}