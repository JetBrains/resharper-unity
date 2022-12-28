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

        protected override void CheckProjectFile(IProjectFile projectItem, Action<TextWriter> test = null)
        {
            if(projectItem.Location.Name.Equals(DotsClassesFileName))
                return;
            base.CheckProjectFile(projectItem, test);
        }

        [Test] public void GenerateBaker01()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
        
        [Test] public void GenerateBaker02()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
        
        [Test] public void GenerateBaker03()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test] public void GenerateBakerWithExistingAuthoringClassInOtherFile01()
        {
            DoNamedTest($"../{DotsClassesFileName}", $"{TestMethod!.Name}_ExistingAuthoringClass.cs");
        }
        
        [Test] public void GenerateBakerWithExistingAuthoringClassAndBakerInOtherFiles()
        {
            DoNamedTest($"../{DotsClassesFileName}"
                , $"{TestMethod!.Name}_ExistingAuthoringClass.cs"
                , $"{TestMethod!.Name}_ExistingBakerClass.cs"
                );
        }
    }
}