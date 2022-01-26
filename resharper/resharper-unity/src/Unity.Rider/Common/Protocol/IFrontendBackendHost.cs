using System;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol
{
    public interface IFrontendBackendHost
    {
        void Do(Action<FrontendBackendModel> action);
    }
}