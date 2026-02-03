using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.CodeAnnotations;
using JetBrains.ReSharper.Psi.CSharp.Impl.ControlFlow.IntValuesAnalysis;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeAnnotations;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class UnityMinAttributeInformationProvider(UnityApi unityApi) : ICustomIntValueRangeAnnotationProvider
{
    public IEnumerable<IClrTypeName> AttributeNames => [KnownTypes.MinAttribute];

    public bool IsApplicable(IAttributesOwner attributesOwner)
    {
        return UnityValueRangeAnnotationUtil.IsApplicable(attributesOwner, unityApi);
    }

    public bool TryApplyAnnotation(IAttributeInstance attributeInstance, AbstractValue.Builder builder)
    {
        if (attributeInstance.PositionParameterCount == 1)
        {
            var unityMinValue = attributeInstance.PositionParameter(0);
            if (unityMinValue.ConstantValue.IsFloat())
            {
                var minLongValue = UnityValueRangeAnnotationUtil.ConvertToLong(unityMinValue.ConstantValue);
                builder.Add(new AbstractValue.LongInterval(minLongValue, long.MaxValue));
                return true;
            }
        }

        return false;
    }
}