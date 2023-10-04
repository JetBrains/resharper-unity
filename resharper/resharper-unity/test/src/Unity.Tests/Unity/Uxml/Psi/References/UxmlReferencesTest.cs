using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.PsiTests.Xaml;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using NUnit.Framework;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References;
using JetBrains.ReSharper.Psi.Xaml.Tree;

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

        protected override bool AcceptReference(IReference reference) =>
            reference is IUxmlNamespaceReference;

        [Test] public void MainMenuTemplate()
        {
            var mainFile = Files.Single(a => a.Name == "MainMenuTemplate.uxml");
            DoTestSolution(ArrayUtil.Add(mainFile.FullPath, Files.Except(mainFile).Select(a=>a.FullPath).ToArray())); 
        }
    }
    
    [TestUnity]
    [TestFileExtension(UxmlProjectFileType.UXML_EXTENSION)]
    public class UxmlReferencesTest2 : XamlReferenceTestWithLibraries
    {
        protected override string RelativeTestDataPath => @"UnityUIElementsCompletionTest";
        
        private VirtualFileSystemPath[] Files =>
            VirtualTestDataPath.Combine("Solutions/UIElementsDemo/")
                .GetChildFiles("*", PathSearchFlags.RecurseIntoSubdirectories).ToArray();

        protected override bool AcceptReference(IReference reference) =>
            reference is ITypeOrNamespaceReference or IXamlNamespaceAliasReference;

        [Test] public void MainMenuTemplate2()
        {
            var mainFile = Files.Single(a => a.Name == "MainMenuTemplate2.uxml");
            DoTestSolution(ArrayUtil.Add(mainFile.FullPath, Files.Except(mainFile).Select(a=>a.FullPath).ToArray())); 
        }
    }
}
