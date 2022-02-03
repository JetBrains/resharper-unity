using System;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRiderTestComponents
{
    [SolutionComponent]
    public class FrontendBackendHostStub : IFrontendBackendHost
    {
        public void Do(Action<FrontendBackendModel> action)
        {
            throw new NotImplementedException();
        }
    }
    
    [SolutionComponent]
    public class BackendUnityHostStub : IBackendUnityHost
    {
        public bool IsConnectionEstablished()
        {
            throw new NotImplementedException();
        }
    }
}