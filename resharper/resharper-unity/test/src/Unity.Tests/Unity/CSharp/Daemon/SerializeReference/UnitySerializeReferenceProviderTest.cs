using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Projects;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.SerializeReference
{
    // public abstract 
    [TestFixture, ReuseSolution(false)]
    public class UnitySerializeReferenceProviderTest : BaseTestWithExistingSolution
    {
        protected override string RelativeTestDataPath => "SerializationReference";
        private const string AssembliesDirectory = "Assemblies";

        [Test]
        public void GenericClassesTest001()
        {
            DoSolutionTestWithGold(@"Solutions\GenericClassesLib01\GenericClassesLib01.sln");
        }

        [Test]
        public void GenericNestedClassesTest002()
        {
            DoSolutionTestWithGold(@"Solutions\GenericClassesLib02\GenericClassesLib02.sln");
        }

        [Test]
        public void GenericNestedClassesTest003()
        {
            DoSolutionTestWithGold(@"Solutions\GenericClassesLib03\GenericClassesLib03.sln");
        }

        [Test]
        public void GenericNestedClassesAssembliesTest004()
        {
            var testSolutionAbsolutePath = GetTestDataFilePath2(@"Solutions\GenericClassesAssembly01\GenericClassesAssembly01.sln");
            SolutionBuilderHelper.PrepareDependencies(BaseTestDataPath, testSolutionAbsolutePath, "GenericClassesLib03", AssembliesDirectory);
            DoSolutionTestWithGold(testSolutionAbsolutePath);
        }

        [Test]
        public void PartialClassesLib01()
        {
            DoSolutionTestWithGold(@"Solutions\PartialClassesLib01\PartialClassesLib01.sln");
        }

        [Test]
        public void WrongSerializeReferenceAttributes()
        {
            DoSolutionTestWithGold(@"Solutions\WrongSerializeReferenceAttributes\WrongSerializeReferenceAttributes.sln");
        }
        
        [Test]
        public void PropertyWithBackingField()
        {
            DoSolutionTestWithGold(@"Solutions\PropertyWithBackingField\PropertyWithBackingField.sln");
        }
        
        [Test]
        public void PropertyWithBackingFieldAssembly()
        {
            var testSolutionAbsolutePath = GetTestDataFilePath2(@"Solutions\PropertyWithBackingFieldAssembly\PropertyWithBackingFieldAssembly.sln");
            SolutionBuilderHelper.PrepareDependencies(BaseTestDataPath, testSolutionAbsolutePath, "PropertyWithBackingField", AssembliesDirectory);
            DoSolutionTestWithGold(testSolutionAbsolutePath);
        }
           
        private void DoSolutionTestWithGold(string solutionPath)
        {
            DoSolutionTestWithGold(GetTestDataFilePath2(solutionPath));
        }
        
        private void DoSolutionTestWithGold(FileSystemPath solutionPath)
        {
            DoTestSolution(solutionPath,
                (lt, solution) =>
                {
                    var swea = SolutionAnalysisService.GetInstance(Solution);
                    using (TestPresentationMap.Cookie())
                    using (swea.RunAnalysisCookie())
                    {
                        swea.ReanalyzeAll();
                        var files = swea.GetFilesToAnalyze().OrderBy(f => f.Name).ToList();
                        foreach (var file in files)
                            swea.AnalyzeInvisibleFile(file);

                        ExecuteWithGold(GetTestDataFilePath2(TestName), writer =>
                        {
                            var unitySerializedReferenceProvider =
                                solution.GetComponent<IUnitySerializedReferenceProvider>();

                            unitySerializedReferenceProvider.DumpFull(writer, solution);
                        });
                    }
                });
        }
    }
}