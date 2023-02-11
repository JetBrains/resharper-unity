using System;
using System.IO;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.FeaturesTestFramework.Generate;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Tests.UnityTestComponents;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Feature.Services.Generate
{
    [TestUnity]
    public class GenerateRefAccessorsTest : GenerateTestBase
    {
        private const string DotsClassesFileName = "DotsClasses.cs";

        protected override string RelativeTestDataPath =>
            @"CSharp\Intentions\QuickFixes\Dots\GenerateRefAccessorsActionFix";

        protected override void CheckProjectFile(IProjectFile projectItem, Action<TextWriter> test = null)
        {
            if (projectItem.Location.Name.Equals(DotsClassesFileName))
                return;
            base.CheckProjectFile(projectItem, test);
        }

        [Test]
        public void GenerateRefROProperties()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test]
        public void GenerateRefRWProperties()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test]
        public void GenerateRefRWGetter()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
    }

    [TestUnity]
    public class GenerateRefAccessorsActionAvailabilityTest
        : ContextActionAvailabilityTestBase<GenerateRefAccessorsAction>
    {
        private const string DotsClassesFileName = "DotsClasses.cs";
        protected override string ExtraPath => String.Empty;

        protected override string RelativeTestDataPath =>
            @"CSharp\Intentions\QuickFixes\Dots\GenerateRefAccessorsActionFix";

        [Test]
        public void GenerateRefRWActionAvailability()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
    }

    [TestUnity]
    public class GenerateRefAccessorsActionExecutionTest
        : ContextActionExecuteTestBase<GenerateRefAccessorsAction>
    {
        private const string DotsClassesFileName = "DotsClasses.cs";
        protected override string ExtraPath => String.Empty;

        protected override string RelativeTestDataPath =>
            @"CSharp\Intentions\QuickFixes\Dots\GenerateRefAccessorsActionFix";


        public GenerateRefAccessorsActionExecutionTest()
        {
            ExecuteBeforeTest = _ =>
            {
                var provider = Solution.GetComponent<TestGenerateRefAccessorsProvider>();
                provider.SelectElement("ScavsCount");

                return Disposable.CreateAction(() => Solution.GetComponent<TestGenerateUnityEventFunctionsProvider>().Clear());
            };
        }
        
        [Test]
        public void GenerateRefRWAction()
        {
            DoNamedTest();
        }

        [Test]
        public void GenerateRefROAction()
        {
            DoNamedTest();
        }
    }
}