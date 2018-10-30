using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Cg.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Resources.Shell;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Cg.ProjectModel
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

        [TestCase(CgProjectFileType.CG_EXTENSION)]
        [TestCase(CgProjectFileType.COMPUTE_EXTENSION)]
        [TestCase(CgProjectFileType.HLSL_EXTENSION)]
        [TestCase(CgProjectFileType.GLSL_EXTENSION)]
        [TestCase(CgProjectFileType.HLSLINC_EXTENSION)]
        [TestCase(CgProjectFileType.GLSLINC_EXTENSION)]
        public void ProjectFileTypeFromExtensionCginc(string extension)
        {
            var projectFileExtensions = Shell.Instance.GetComponent<IProjectFileExtensions>();
            Assert.AreSame(CgProjectFileType.Instance, projectFileExtensions.GetFileType(extension));
        }
    }
}