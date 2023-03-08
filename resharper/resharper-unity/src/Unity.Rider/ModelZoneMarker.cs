using JetBrains.Application.BuildScript.Application.Zones;

// ReSharper disable once CheckNamespace
namespace JetBrains.Rider.Model.Unity
{
    // Zone marker for the generated models. Required to keep zone inspections happy because the generated code uses a
    // type from a namespace that requires IRiderModelZone. In practice, this doesn't cause issues
    // TODO: Look at moving the models to a better location or namespace?
    [ZoneMarker]
    public class ZoneMarker : IRequire<IRiderModelZone>
    {
    }
}