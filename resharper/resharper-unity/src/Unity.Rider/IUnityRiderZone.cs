using JetBrains.Application.BuildScript.Application.Zones;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    // This zone is not IRequire<>'d anywhere!
    // It is defined purely so that the Rider tests can have a direct type reference. Without a direct reference, this
    // assembly is not included in container composition and components aren't loaded.
    // TODO: Test what happens in an actual ReSharper installation. Do we still need a direct reference?
    // TODO: Is this a hack? Is there a better way to do this?
    // TODO: Should we actually have this zone? How would it get activated?
    [ZoneDefinition]
    public interface IUnityRiderZone : IZone
    {
    }
}