﻿using System.Collections.Generic;
using System.Linq;
using JetBrains.DocumentManagers;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Tests.TestFramework;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TextControl;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Feature.Services.Refactorings
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefRenameTests : RenameTestBase
    {
        protected override string RelativeTestDataPath => @"AsmDef\Refactorings\Rename";

        [Test] public void TestSingleFile() { DoNamedTest2(); }
        [Test] public void TestCrossFileRename() { DoTestSolution([TestName2], ["CrossFileRename_SecondProject.asmdef"]); }
        [Test] public void TestRenameFile() { DoNamedTest2(); }
        [Test] public void TestGuidReference() { DoTestSolution([TestName2, "GuidReference.asmdef.meta"], ["GuidReference_SecondProject.asmdef"]); }

        protected override void AdditionalTestChecks(ITextControl textControl, IProject project)
        {
            var solution = project.GetSolution();
            foreach (var topLevelProject in solution.GetTopLevelProjects())
            {
                if (topLevelProject.IsProjectFromUserView() && !Equals(topLevelProject, project))
                {
                    foreach (var projectFile in topLevelProject.GetSubItems().OfType<IProjectFile>())
                    {
                        ExecuteWithGold(projectFile, writer =>
                        {
                            var document = projectFile.GetDocument();
                            writer.Write(document.GetText());
                        });
                    }

                    // TODO: Should really recurse into child folders, but not used by these tests
                }
            }
        }
    }
}