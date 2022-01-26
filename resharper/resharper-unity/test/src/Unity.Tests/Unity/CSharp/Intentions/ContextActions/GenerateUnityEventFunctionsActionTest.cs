using System.Collections.Generic;
using System.Linq;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Tests.UnityTestComponents;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate;
using JetBrains.ReSharper.Psi.CSharp;
using NuGet.Packaging;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class GenerateUnityEventFunctionsActionAvailabilityTest
        : ContextActionAvailabilityTestBase<GenerateUnityEventFunctionsAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "GenerateUnityEventFunctions";

        [Test] public void TestAvailability01() { DoNamedTest2(); }
    }

    [TestUnity]
    public class GenerateUnityEventFunctionsActionExecutionTest
        : ContextActionExecuteTestBase<GenerateUnityEventFunctionsAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "GenerateUnityEventFunctions";

        public GenerateUnityEventFunctionsActionExecutionTest()
        {
            ExecuteBeforeTest = _ =>
            {
                var provider = Solution.GetComponent<TestGenerateUnityEventFunctionsProvider>();
                provider.SelectElement("Awake");
                provider.SelectElement("Update");
                provider.SelectElement("LateUpdate");

                return Disposable.CreateAction(() => Solution.GetComponent<TestGenerateUnityEventFunctionsProvider>().Clear());
            };
        }

        [Test] public void TestGenerateEvents() { DoNamedTest2(); }
        [Test] public void TestGenerateEventsAtCaretLocation() { DoNamedTest2(); }
    }
}