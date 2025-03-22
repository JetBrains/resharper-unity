using JetBrains.Application.Parts;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol
{
    [DerivedComponentsInstantiationRequirement(InstantiationRequirement.DeadlockSafe)]
    public interface IBackendUnityHost
    {
        bool IsConnectionEstablished();
    }
}