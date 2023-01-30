using System;
using System.IO;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.FeaturesTestFramework.Generate;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Feature.Services.Generate
{
    [TestUnity]
    public class GenerateBakerAndAuthoringActionAvailabilityTest  : GenerateTestBase
    {
        private const string DotsClassesFileName = "DotsClasses.cs";
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\Dots\GenerateBakerAndAuthoringActionFix";

        protected override void CheckProjectFile(IProjectFile projectItem, Action<TextWriter>? test = null)
        {
            if(projectItem.Location.Name.Equals(DotsClassesFileName))
                return;
            base.CheckProjectFile(projectItem, test);
        }

        [Test] public void GenerateNewBakerNotNested()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test] public void GenerateNewBakerNested()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test] public void AddNewComponentToBaker()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test] public void UpdateExistingNestedBaker()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test] public void CreateNewWithExistingBaker()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test] public void AuthoringAndBakerInOtherFiles()
        {
            DoNamedTest($"../{DotsClassesFileName}"
                , $"{TestMethod!.Name}_Authoring.cs"
                , $"{TestMethod!.Name}_Baker.cs"
                );
        }
    }
}