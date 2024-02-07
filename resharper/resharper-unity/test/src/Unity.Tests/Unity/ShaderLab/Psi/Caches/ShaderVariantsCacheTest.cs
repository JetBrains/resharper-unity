using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Psi.Caches
{
    [RequireHlslSupport]
    [Category("Caches")]
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderVariantsCacheTest : BaseTestWithSingleProject
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Psi\Caches\ShaderVariants";

        [TestCase("AllDirectives")]
        [TestCase("DirectivesWithComments")]
        public void TestCacheItem(string testName) => DoOneTest(testName);
        
        protected override void DoTest(Lifetime lifetime, IProject testProject)
        {
            var projectFiles = testProject.GetAllProjectFiles().ToList();
            Assert.True(Enumerable.Any(projectFiles), "no project files to test");
            
            foreach (var projectFile in projectFiles)
                CheckOutput(testProject, projectFile.ToSourceFile().NotNull("sourceFile != null"));
        }
        
        private void CheckOutput(IProject project, IPsiSourceFile sourceFile)
        {
            ExecuteWithGold(sourceFile, textWriter =>
            {
                var cache = project.GetComponent<ShaderProgramCache>();
                var locationTracker = Solution.GetComponent<InjectedHlslFileLocationTracker>();
                var fileLocations = locationTracker.GetActualFileLocations(sourceFile);
                var keywords = new SortedSet<string>();
                foreach (var location in fileLocations)
                {
                    if (cache.TryGetShaderProgramInfo(location, out var programInfo) && programInfo.ShaderFeatures is { IsEmpty: false } shaderFeatures)
                        keywords.AddRange(shaderFeatures.SelectMany(f => f.Entries).Select(e => e.Keyword));
                }
                foreach (var keyword in keywords)
                    textWriter.WriteLine(keyword);
            });
        }
    }
}