using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.ExternalSources.ILViewer;

namespace JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.ILViewer
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IIlViewerZone>
    {
    }
}