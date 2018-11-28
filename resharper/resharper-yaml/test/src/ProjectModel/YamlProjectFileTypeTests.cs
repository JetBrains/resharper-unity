using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Yaml.Tests.ProjectModel
{
  [TestFixture]
  public class YamlProjectFileTypeTests : BaseTest
  {
    [Test]
    public void ProjectFileTypeIsRegistered()
    {
      Assert.NotNull(YamlProjectFileType.Instance);

      var projectFileTypes = Shell.Instance.GetComponent<IProjectFileTypes>();
      Assert.NotNull(projectFileTypes.GetFileType(YamlProjectFileType.Name));
    }

    [Test]
    public void ProjectFileTypeFromExtension()
    {
      var projectFileExtensions = Shell.Instance.GetComponent<IProjectFileExtensions>();
      // Note that even though Rider doesn't register the .yaml file extension, we do register it for testing...
      Assert.AreSame(YamlProjectFileType.Instance, projectFileExtensions.GetFileType(YamlProjectFileType.YAML_EXTENSION));
    }
  }
}