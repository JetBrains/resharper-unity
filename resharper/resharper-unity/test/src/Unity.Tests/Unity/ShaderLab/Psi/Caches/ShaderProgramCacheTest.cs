using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Psi.Caches
{
    [RequireHlslSupport]
    [Category("Caches")]
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderProgramCacheTest : BaseTestWithSingleProject
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Psi\Caches\ShaderProgram";
        
        [Test]
        public void TestMultipleSourceFilesWithSamePath()
        {
            DoTestSolution(_ => CreateSolutionConfiguration(new[] { "TestMultipleSourceFilesWithSamePath.shader", "TestMultipleSourceFilesWithSamePath.shader" }), (lt, solution) =>
            {
                DoTest(lt);
                
                var testProject = solution.GetProjectByName("TestProject");
                var firstProjectFile = testProject.GetAllProjectFiles().First();
                var firstSourceFile = firstProjectFile.ToSourceFile();
                Assert.NotNull(firstSourceFile);
                var shaderProgramCache = solution.GetComponent<ShaderProgramCache>();
                shaderProgramCache.MarkAsDirty(firstSourceFile);
                shaderProgramCache.SyncUpdate(false);
                DoTest(lt);
                
                ((ProjectItemBase)firstProjectFile).DoRemove();
                shaderProgramCache.SyncUpdate(false);
                DoTest(lt);
                
                firstProjectFile = testProject.GetAllProjectFiles().First();
                ((ProjectItemBase)firstProjectFile).DoRemove();
                shaderProgramCache.SyncUpdate(false);
                shaderProgramCache.ForEachLocation(_ => Assert.Fail("All locations should be removed"));
            });
        }
        
        protected override void DoTest(Lifetime lifetime, IProject testProject)
        {
            var projectFiles = testProject.GetAllProjectFiles().ToList();
            Assert.True(projectFiles.Any(), "no project files to test");
            
            foreach (var projectFile in projectFiles)
                CheckOutput(testProject, projectFile.ToSourceFile().NotNull("sourceFile != null"));
        }
        
        private void CheckOutput(IProject project, IPsiSourceFile sourceFile)
        {
            ExecuteWithGold(sourceFile, textWriter =>
            {
                var cache = project.GetComponent<ShaderProgramCache>();
                cache.ForEachLocation(location => textWriter.WriteLine($"{location.Name}{location.RootRange}"));
            });
        }
    }
}