using System;
using System.IO;
using System.Linq;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Tests.UnityTestComponents;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Feature.Services.Generate
{
    [TestUnity]
    public class AddFieldToExistingBakerAvailabilityTest : ContextActionAvailabilityTestBase<AddFieldToExistingBakerAndAuthoringAction>
    {
        private const string DotsClassesFileName = "DotsClasses.cs";
        protected override string ExtraPath => String.Empty;

        protected override string RelativeTestDataPath =>
            @"CSharp\Intentions\QuickFixes\Dots\AddFieldToExistingBaker";


        protected override void DoTest(Lifetime lifetime, IProject project)
        {
            using (UnityPackageCookie.RunUnityPackageCookie(Solution, PackageManager.UnityEntitiesPackageName))
                base.DoTest(lifetime, project);
        }

        [Test]
        public void AddFieldActionAvailability()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
    }

    [TestUnity]
    public class AddFieldToExistingBakerExecutionTest : ContextActionExecuteTestBase<AddFieldToExistingBakerAndAuthoringAction>
    {
        private const string DotsClassesFileName = "DotsClasses.cs";
        protected override string ExtraPath => String.Empty;

        protected override string RelativeTestDataPath =>
            @"CSharp\Intentions\QuickFixes\Dots\AddFieldToExistingBaker";

        protected override void CheckProjectFile(IProjectFile projectItem, Action<TextWriter>? test = null)
        {
            if(projectItem.Location.Name.Equals(DotsClassesFileName))
                return;
            base.CheckProjectFile(projectItem, test);
        }

        protected override void CheckDocument(TextWriter dumpWriter, IDocument document, IProjectFile projectFile)
        {
            if (projectFile.Name.Contains(DotsClassesFileName))
            {
                return;
            }
            base.CheckDocument(dumpWriter, document, projectFile);
        }

        protected override void DoTest(Lifetime lifetime, IProject project)
        {
            using (UnityPackageCookie.RunUnityPackageCookie(Solution, PackageManager.UnityEntitiesPackageName))
                base.DoTest(lifetime, project);
        }

        [Test]
        public void AddFieldToBaker1()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test]
        public void AddFieldToBaker2()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test]
        public void AddFieldToBaker3()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test]
        public void AddFieldToBaker4()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
    }
}