using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml;

namespace JetBrains.ReSharper.Plugins.Tests.YamlTestComponents
{
  // Zone requirements for non-environment test components. Separate namespace to environment components to avoid
  // adding inactive zones as requirements to environment components
  [ZoneMarker]
  public class ZoneMarker : IRequire<IProjectModelZone>, IRequire<ILanguageYamlZone>
  {
  }
}