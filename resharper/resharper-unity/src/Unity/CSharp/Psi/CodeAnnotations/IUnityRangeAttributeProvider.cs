using JetBrains.Application.Parts;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeAnnotations;

[DerivedComponentsInstantiationRequirement(InstantiationRequirement.DeadlockSafe)]
public interface IUnityRangeAttributeProvider
{
    public bool IsApplicable(IAttributeInstance attributeInstance);
    public long GetMinValue(IAttributeInstance attributeInstance);
    public long GetMaxValue(IAttributeInstance attributeInstance);
}