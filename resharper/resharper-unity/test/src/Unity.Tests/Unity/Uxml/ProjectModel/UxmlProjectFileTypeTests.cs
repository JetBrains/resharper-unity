using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi;
using JetBrains.ReSharper.Resources.Shell;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Uxml.ProjectModel
{
    [TestFixture]
    public class UxmlProjectFileTypeTests
    {
        [Test]
        public void ProjectFileTypeIsRegistered()
        {
            Assert.NotNull(UxmlProjectFileType.Instance);

            var projectFileTypes = Shell.Instance.GetComponent<IProjectFileTypes>();
            Assert.NotNull(projectFileTypes.GetFileType(UxmlProjectFileType.Name));
        }

        [TestCase(UxmlProjectFileType.UXML_EXTENSION)]
        public void ProjectFileTypeFromExtension(string extension)
        {
            var projectFileExtensions = Shell.Instance.GetComponent<IProjectFileExtensions>();
            Assert.AreSame(UxmlProjectFileType.Instance, projectFileExtensions.GetFileType(extension));
        }
    }
}
