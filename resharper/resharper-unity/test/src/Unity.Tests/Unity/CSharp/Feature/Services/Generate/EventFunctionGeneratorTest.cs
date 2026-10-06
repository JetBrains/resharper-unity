using JetBrains.ReSharper.FeaturesTestFramework.Generate;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Feature.Services.Generate
{
    [TestUnity]
    public class EventFunctionGeneratorTest : GenerateTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Generate";

        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements01() { DoNamedTest(); }
        // TODO: Deriving from AssetModificationProcessor doesn't work. Don't know why
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements04() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements05() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements06() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements07() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements08() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements09() { DoNamedTest(); }
        // ListElements10 uses a type that's moved namespace in other versions
        [Test, TestUnity(UnityVersion.Unity54)]
        [UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")]
        public void ListElements10() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements11() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements12() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2017_4), UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements13() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2017_4), UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements14() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements15() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements16() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements17() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void ListElements19() { DoNamedTest(); }

        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Insert generated methods")] public void MonoBehaviour01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void HasExistingMethods() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void HasExistingBaseFunctions() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Available elements list")] public void HasExistingVirtualFunction() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Insert generated methods")] public void InsertSingleMethod01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Insert generated methods")] public void InsertSingleMethod02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Insert generated methods")] public void InsertMultipleMethods() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Insert generated methods")] public void InsertWithExistingMethods() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Insert generated methods")] public void InsertWithBaseVirtualFunctions01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Insert generated methods")] public void InsertWithBaseVirtualFunctions02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Insert generated methods")] public void InsertWithBaseVirtualFunctions03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Insert generated methods")] public void InsertStaticMethod() { DoNamedTest(); }

        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Insert generated methods")] public void ResolvesNamespacesGlobally() { DoNamedTest(); }

        // It would be nice if the base test distinguished between unavailable, no items and disabled
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Insert generated methods")] public void NonUnityType() { DoNamedTest(); }
    }

    public class EventFunctionGeneratorNonUnityProjectTest : GenerateTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Generate";

        // It would be nice if the base test distinguished between unavailable, no items and disabled
        [Test, UnityPluginBackendChecklist("Code generation", "Generate event functions", "Insert generated methods")] public void NonUnityProject() { DoNamedTest(); }
    }
}