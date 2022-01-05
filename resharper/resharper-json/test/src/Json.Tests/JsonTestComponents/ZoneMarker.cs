using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Tests.JsonTestComponents
{
    // Zone requirements for non-environment test components. Separate namespace to environment components to avoid
    // adding inactive zones as requirements to environment components
    [ZoneMarker]
    public class ZoneMarker : IRequire<IProjectModelZone>
    {
    }
}