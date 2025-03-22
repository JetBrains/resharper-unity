using System;
using JetBrains.Application.Parts;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol
{
    [DerivedComponentsInstantiationRequirement(InstantiationRequirement.DeadlockSafe)]
    public interface IFrontendBackendHost
    {
        void Do(Action<FrontendBackendModel> action);
    }
}