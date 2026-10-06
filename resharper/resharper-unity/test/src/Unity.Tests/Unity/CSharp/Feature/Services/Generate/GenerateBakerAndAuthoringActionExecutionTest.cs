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

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and authoring component")] public void GenerateNewBakerNotNested()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and authoring component")] public void GenerateNewBakerNested()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and authoring component")] public void GenerateNewBakerForEmptyComponent()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test]
        [UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and authoring component")]
        public void GenerateNewBakerForEmptyComponentClass()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
        
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and authoring component")] public void UpdateBakerForExistingEmptyComponent()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
        
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and authoring component")] public void GenerateNewBakerForComponentWithValue()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and authoring component")] public void AddNewComponentToBaker()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and authoring component")] public void UpdateExistingNestedBaker()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
        
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and authoring component")] public void UpdateExistingPartialBaker()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and authoring component")] public void CreateNewWithExistingBaker()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
        
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and authoring component")] public void CreateNewNestedWithExistingBaker()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and authoring component")] public void AuthoringAndBakerInOtherFiles()
        {
            DoNamedTest($"../{DotsClassesFileName}"
                , $"{TestMethod!.Name}_Authoring.cs"
                , $"{TestMethod!.Name}_Baker.cs"
                );
        }
    }
}