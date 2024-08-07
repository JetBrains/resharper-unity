using System.Linq;
using JetBrains.ReSharper.Plugins.Tests.TestFramework;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Projects;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Uxml.Psi.References;

[TestUnity(UnityVersion.Unity2022_3)]
[TestFileExtension(".cs")]
public class UxmlReferenceRenameTest : RenameTestBase
{
    protected override string RelativeTestDataPath => @"UnityUIElementsCompletionTest";
    protected override string SolutionFileName => SolutionItemsBasePath.Combine("Solutions/UIElementsDemo/UIElementsDemo.sln").FullPath;

    [Test]
    public void Rename01()
    {
        DoNamedTest("Rename01MainMenuTemplate.uxml");
    }
}