using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Features.ReSpeller;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Internal
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IInternalVisibilityZone>, IRequire<IReSpellerZone>
    {
        
    }
}