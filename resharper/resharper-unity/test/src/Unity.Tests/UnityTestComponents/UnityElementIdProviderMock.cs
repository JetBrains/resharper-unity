using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityElementIdProviderMock : UnityElementIdProvider
    {
        protected override void CalculatePsiModuleHash(ref Hash hash, IPsiModule psiModule, bool isCompiledType)
        {
            if (!isCompiledType)
                base.CalculatePsiModuleHash(ref hash, psiModule, isCompiledType);
            else
            {
                hash.PutInt(43);
                hash.PutString(psiModule.Name);
                hash.PutString(psiModule.DisplayName);
            }
        }
    }
}