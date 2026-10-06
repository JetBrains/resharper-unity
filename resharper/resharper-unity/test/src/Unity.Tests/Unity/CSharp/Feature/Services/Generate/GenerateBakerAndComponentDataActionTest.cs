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

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and component data")] public void GenerateComponentAndBaker()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
        
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and component data")] public void GenerateEmptyComponentAndBaker()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
        
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and component data")] public void GenerateToExistingComponent()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
                
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and component data")] public void NewComponentToExistingBaker()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and component data")] public void ExistingBakerWithCustomGetEntity()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and component data")] public void ExistingBakerWithCustomGetEntity2()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and component data")] public void ExistingBakerWithCustomGetEntity3()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
                
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and component data")] public void ExistingBakerAndComponent()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }
                
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and component data")] public void ExistingBakerAndComponentObject()
        {
            DoNamedTest($"../{DotsClassesFileName}");
        }

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Code generation", "Generate baker and component data")] public void ComponentAndBakerInOtherFiles()
        {
            DoNamedTest($"../{DotsClassesFileName}"
                , $"{TestMethod!.Name}_Baker.cs"
                , $"{TestMethod!.Name}_Component.cs"
            );
        }
    }
}