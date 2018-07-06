#if !RIDER

using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.VsIntegration.Shell.Zones;

// It would be nice to use zones properly here, but
// a) it's really hard :(
// b) Code Cleanup uses XAML serialisers that don't respect zones and try
//    to resolve all types causing problems for Rider, which doesn't have
//    any VS libraries, such as VS.Platform.Core

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IVisualStudioEnvZone>
    {
    }
}

#endif
