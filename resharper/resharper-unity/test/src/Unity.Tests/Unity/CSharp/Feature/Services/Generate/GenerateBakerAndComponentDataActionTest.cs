using System;
using System.IO;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.FeaturesTestFramework.Generate;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Feature.Services.Generate
{
    [TestUnity]
    public class GenerateBakerAndComponentDataActionTest  : GenerateTestBase
    {
        private const string DotsClassesFileName = "DotsClasses.cs";
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\Dots\GenerateBakerAndComponentData";

        protected override void CheckProjectFile(IProjectFile projectItem, Action<TextWriter>? test = null)
        {
            if(projectItem.Location.Name.Equals(DotsClassesFileName))
                return;
            base.CheckProjectFile(projectItem, test);
        }

        [Test] public void GenerateComponentAndBaker()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
        
        [Test] public void GenerateEmptyComponentAndBaker()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
        
        [Test] public void GenerateToExistingComponent()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
                
        [Test] public void NewComponentToExistingBaker()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test] public void ExistingBakerWithCustomGetEntity()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test] public void ExistingBakerWithCustomGetEntity2()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test] public void ExistingBakerWithCustomGetEntity3()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
                
        [Test] public void ExistingBakerAndComponent()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test] public void ComponentAndBakerInOtherFiles()
        {
            DoNamedTest($"../{DotsClassesFileName}"
                , $"{TestMethod!.Name}_Baker.cs"
                , $"{TestMethod!.Name}_Component.cs"
            );
        }
    }
}