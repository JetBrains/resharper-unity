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
public class OdinMaxAttributeInformationProvider(UnityApi unityApi) : ICustomIntValueRangeAnnotationProvider
{
    public IEnumerable<IClrTypeName> AttributeNames => [OdinKnownAttributes.MaxValueAttribute];

    public bool IsApplicable(IAttributesOwner attributesOwner)
    {
        return UnityValueRangeAnnotationUtil.IsApplicable(attributesOwner, unityApi);
    }

    public bool TryApplyAnnotation(IAttributeInstance attributeInstance, AbstractValue.Builder builder)
    {
        if (attributeInstance.PositionParameterCount == 1)
        {
            var attributeValue = attributeInstance.PositionParameter(0);
            if (OdinCodeAnnotationUtil.IsApplicable(attributeValue.ConstantValue))
            {
                var maxValue = OdinCodeAnnotationUtil.GetMaxValue(attributeValue.ConstantValue);
                builder.Add(new AbstractValue.LongInterval(long.MinValue, maxValue));
                return true;
            }
        }

        return false;
    }
}