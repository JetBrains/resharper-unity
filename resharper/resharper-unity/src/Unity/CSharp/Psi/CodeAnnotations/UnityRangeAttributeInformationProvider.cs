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
public class UnityRangeAttributeInformationProvider(UnityApi unityApi) : ICustomIntValueRangeAnnotationProvider
{
    public IEnumerable<IClrTypeName> AttributeNames => [KnownTypes.RangeAttribute];

    public bool IsApplicable(IAttributesOwner attributesOwner)
    {
        return UnityValueRangeAnnotationUtil.IsApplicable(attributesOwner, unityApi);
    }

    public bool TryApplyAnnotation(IAttributeInstance attributeInstance, AbstractValue.Builder builder)
    {
        if (attributeInstance.PositionParameterCount == 2)
        {
            var unityFrom = attributeInstance.PositionParameter(0);
            var unityTo = attributeInstance.PositionParameter(1);

            if (unityFrom.ConstantValue.IsFloat() && unityTo.ConstantValue.IsFloat())
            {
                //
                // Take the floor, because that's how Unity works.
                // E.g. Unity's Inspector treats [Range(1.7f, 10.9f)] as between 1 and 10 inclusive
                //

                var fromLongValue = UnityValueRangeAnnotationUtil.ConvertToLong(unityFrom.ConstantValue);
                var toLongValue = UnityValueRangeAnnotationUtil.ConvertToLong(unityTo.ConstantValue);

                if (fromLongValue <= toLongValue)
                {
                    builder.Add(new AbstractValue.LongInterval(fromLongValue, toLongValue));
                    return true;
                }
            }
        }

        return false;
    }
}