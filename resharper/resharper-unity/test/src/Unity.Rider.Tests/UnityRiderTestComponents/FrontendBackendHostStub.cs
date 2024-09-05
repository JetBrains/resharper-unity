using System;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRiderTestComponents
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class FrontendBackendHostStub : IFrontendBackendHost
    {
        public void Do(Action<FrontendBackendModel> action)
        {
            throw new NotImplementedException();
        }
    }

    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class BackendUnityHostStub : IBackendUnityHost
    {
        public bool IsConnectionEstablished()
        {
            throw new NotImplementedException();
        }
    }
}