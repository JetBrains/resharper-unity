using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.PsiTests.Xaml;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Uxml
{
    [TestUnity]
    [TestFileExtension(UxmlProjectFileType.UXML_EXTENSION)]
    public class UnityReferencesTest : XamlReferenceTestWithLibraries
    {
        protected override string RelativeTestDataPath => @"UnityUIElementsCompletionTest";
        protected override bool AcceptReference(IReference reference) => true;

        [Test] public void MainMenuTemplate() { DoNamedTest(); }
    }
}
