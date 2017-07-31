using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Resources.Shell;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ProjectModel
{
    [TestFixture]
    public class CgProjectFileTypeTests
    {
        [Test]
        public void ProjectFileTypeIsRegistered()
        {
            Assert.NotNull(CgProjectFileType.Instance);

            var projectFileTypes = Shell.Instance.GetComponent<IProjectFileTypes>();
            Assert.NotNull(projectFileTypes.GetFileType(CgProjectFileType.Name));
        }

        [Test]
        public void ProjectFileTypeFromExtension()
        {
            var projectFileExtensions = Shell.Instance.GetComponent<IProjectFileExtensions>();
            Assert.AreSame(CgProjectFileType.Instance, projectFileExtensions.GetFileType(CgProjectFileType.CG_EXTENSION));
        }
    }
}