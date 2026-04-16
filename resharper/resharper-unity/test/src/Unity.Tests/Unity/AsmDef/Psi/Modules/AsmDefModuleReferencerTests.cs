using System.Linq;
using JetBrains.DocumentManagers;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Modules;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Psi.Modules
{
    // Tests that AsmDefModuleReferencer correctly writes to the .asmdef file when a reference is added via
    // the module referencer mechanism (e.g. Alt+Enter "Add reference" on an unresolved type).
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefModuleReferencerTests : BaseTestWithSingleProject
    {
        protected override string RelativeTestDataPath => @"AsmDef\Psi\Modules\AsmDefModuleReferencer";
        
        // protected override string ProjectName => "TestAddReferenceByName";
        // protected override string SecondProjectName => "TestAddReferenceByName_Target";

        [Test]
        public void TestAddReferenceByName()
        {
            ProjectName = "TestAddReferenceByName";
            SecondProjectName = "TestAddReferenceByName_Target";
            DoTestSolution(["TestAddReferenceByName.asmdef"], ["TestAddReferenceByName_Target.asmdef"]);
        }

        [Test]
        public void TestAddReferenceByGuid()
        {
            ProjectName = "TestAddReferenceByGuid";
            SecondProjectName = "TestAddReferenceByGuid_Target";
            DoTestSolution(
                ["TestAddReferenceByGuid.asmdef"],
                ["TestAddReferenceByGuid_Target.asmdef", "TestAddReferenceByGuid_Target.asmdef.meta"]);
        }

        protected override void DoTest(Lifetime lifetime, IProject testProject)
        {
            var solution = testProject.GetSolution();
            var psiServices = solution.GetPsiServices();
            psiServices.Files.CommitAllDocuments();

            var targetProject = solution.GetTopLevelProjects()
                .First(p => p.IsProjectFromUserView() && !Equals(p, testProject));

            var targetFrameworkId = testProject.TargetFrameworkIds.First();
            var sourceModule = psiServices.Modules.GetPrimaryPsiModule(testProject, targetFrameworkId)!;
            var targetModule = psiServices.Modules.GetPrimaryPsiModule(targetProject, targetFrameworkId)!;

            var referencer = PsiShared.GetComponent<AsmDefModuleReferencer>();

            Assert.IsTrue(referencer.CanReferenceModule(sourceModule, targetModule, null),
                "AsmDef-to-AsmDef reference should be supported by AsmDefModuleReferencer");

            using (WriteLockCookie.Create())
                referencer.ReferenceModule(sourceModule, targetModule);

            psiServices.Files.CommitAllDocuments();

            // Verify the source .asmdef file was updated
            foreach (var projectFile in testProject.GetSubItems().OfType<IProjectFile>()
                         .Where(f => f.LanguageType.Is<AsmDefProjectFileType>()))
            {
                ExecuteWithGold(projectFile, writer => writer.Write(projectFile.GetDocument().GetText()));
            }
        }
    }
}
