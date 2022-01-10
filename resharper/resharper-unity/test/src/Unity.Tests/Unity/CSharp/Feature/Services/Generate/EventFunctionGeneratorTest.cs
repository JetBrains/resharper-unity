using JetBrains.ReSharper.FeaturesTestFramework.Generate;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Feature.Services.Generate
{
    [TestUnity]
    public class EventFunctionGeneratorTest : GenerateTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Generate";

        [Test] public void ListElements01() { DoNamedTest(); }
        // TODO: Deriving from AssetModificationProcessor doesn't work. Don't know why
        [Test] public void ListElements02() { DoNamedTest(); }
        [Test] public void ListElements03() { DoNamedTest(); }
        [Test] public void ListElements04() { DoNamedTest(); }
        [Test] public void ListElements05() { DoNamedTest(); }
        [Test] public void ListElements06() { DoNamedTest(); }
        [Test] public void ListElements07() { DoNamedTest(); }
        [Test] public void ListElements08() { DoNamedTest(); }
        [Test] public void ListElements09() { DoNamedTest(); }
        // ListElements10 uses a type that's moved namespace in other versions
        [Test, TestUnity(UnityVersion.Unity54)]
        public void ListElements10() { DoNamedTest(); }
        [Test] public void ListElements11() { DoNamedTest(); }
        [Test] public void ListElements12() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2017_4)] public void ListElements13() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2017_4)] public void ListElements14() { DoNamedTest(); }
        [Test] public void ListElements15() { DoNamedTest(); }
        [Test] public void ListElements16() { DoNamedTest(); }
        [Test] public void ListElements17() { DoNamedTest(); }
        [Test] public void ListElements19() { DoNamedTest(); }

        [Test] public void MonoBehaviour01() { DoNamedTest(); }
        [Test] public void HasExistingMethods() { DoNamedTest(); }
        [Test] public void HasExistingBaseFunctions() { DoNamedTest(); }
        [Test] public void HasExistingVirtualFunction() { DoNamedTest(); }
        [Test] public void InsertSingleMethod01() { DoNamedTest(); }
        [Test] public void InsertSingleMethod02() { DoNamedTest(); }
        [Test] public void InsertMultipleMethods() { DoNamedTest(); }
        [Test] public void InsertWithExistingMethods() { DoNamedTest(); }
        [Test] public void InsertWithBaseVirtualFunctions01() { DoNamedTest(); }
        [Test] public void InsertWithBaseVirtualFunctions02() { DoNamedTest(); }
        [Test] public void InsertWithBaseVirtualFunctions03() { DoNamedTest(); }
        [Test] public void InsertStaticMethod() { DoNamedTest(); }

        [Test] public void ResolvesNamespacesGlobally() { DoNamedTest(); }

        // It would be nice if the base test distinguished between unavailable, no items and disabled
        [Test] public void NonUnityType() { DoNamedTest(); }
    }

    public class EventFunctionGeneratorNonUnityProjectTest : GenerateTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Generate";

        // It would be nice if the base test distinguished between unavailable, no items and disabled
        [Test] public void NonUnityProject() { DoNamedTest(); }
    }
}