using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Xaml.Impl.Tree.References;
using JetBrains.ReSharper.PsiTests.Xaml;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Uxml.Psi.References
{
    [TestUnity]
    [TestFileExtension(UxmlProjectFileType.UXML_EXTENSION)]
    public class UxmlReferencesTest : XamlReferenceTestWithLibraries
    {
        protected override string RelativeTestDataPath => @"UnityUIElementsCompletionTest";
        
        private VirtualFileSystemPath[] Files =>
            VirtualTestDataPath.Combine("Solutions/UIElementsDemo/")
                .GetChildFiles("*", PathSearchFlags.RecurseIntoSubdirectories).ToArray();
        
        protected override bool AcceptReference(IReference reference) => reference is IXamlNamespaceReference;

        [Test] public void MainMenuTemplate()
        {
            var mainFile = Files.Single(a => a.Name == "MainMenuTemplate.uxml");
            DoTestSolution(ArrayUtil.Add(mainFile.FullPath, Files.Except(mainFile).Select(a=>a.FullPath).ToArray())); 
        }
    }
}
