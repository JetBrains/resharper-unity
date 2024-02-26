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
    public class GenerateComponentReferencesActionAvailabilityTest : ContextActionAvailabilityTestBase<GenerateComponentReferencesAction>
    {
        private const string DotsClassesFileName = "DotsClasses.cs";
        protected override string ExtraPath => String.Empty;

        protected override string RelativeTestDataPath =>
            @"CSharp\Intentions\QuickFixes\Dots\GenerateComponentReferencesActionFix";

        [Test]
        public void GenerateComponentRefActionAvailability()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
    }

    [TestUnity]
    public class GenerateComponentReferencesActionExecutionTest : ContextActionExecuteTestBase<GenerateComponentReferencesAction>
    {
        private const string DotsClassesFileName = "DotsClasses.cs";
        protected override string ExtraPath => String.Empty;

        protected override string RelativeTestDataPath =>
            @"CSharp\Intentions\QuickFixes\Dots\GenerateComponentReferencesActionFix";


        public GenerateComponentReferencesActionExecutionTest()
        {
            ExecuteBeforeTest = _ =>
            {
                var provider = Solution.GetComponent<TestGenerateComponentReferencesProvider>();
                provider.SelectElement("Factory4");
                
                return Disposable.CreateAction(() => provider.Clear());
            };
        }

        [Test]
        public void GenerateRefROAction()
        {
            DoNamedTest();
        }
    }
}