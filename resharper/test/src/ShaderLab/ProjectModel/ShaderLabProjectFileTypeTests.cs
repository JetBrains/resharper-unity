using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.ProjectModel
{
    [TestFixture]
    public class ShaderLabProjectFileTypeTests : BaseTest
    {
        [Test]
        public void ProjectFileTypeIsRegistered()
        {
            Assert.NotNull(ShaderLabProjectFileType.Instance);

            var projectFileTypes = Shell.Instance.GetComponent<IProjectFileTypes>();
            Assert.NotNull(projectFileTypes.GetFileType(ShaderLabProjectFileType.Name));
        }

        [Test]
        public void ProjectFileTypeFromExtension()
        {
            var projectFileExtensions = Shell.Instance.GetComponent<IProjectFileExtensions>();
            Assert.AreSame(ShaderLabProjectFileType.Instance, projectFileExtensions.GetFileType(ShaderLabProjectFileType.SHADERLAB_EXTENSION));
        }
    }
}