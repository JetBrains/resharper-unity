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
  }
}