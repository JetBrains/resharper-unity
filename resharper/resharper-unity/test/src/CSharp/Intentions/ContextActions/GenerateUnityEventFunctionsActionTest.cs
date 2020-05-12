using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate;
using JetBrains.ReSharper.Psi.CSharp;
using NuGet.Packaging;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.ContextActions
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
            ExecuteBeforeTest = solution =>
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

    [GeneratorElementProvider(GeneratorUnityKinds.UnityEventFunctions, typeof(CSharpLanguage))]
    public class TestGenerateUnityEventFunctionsProvider : IGeneratorElementProvider
    {
        private readonly JetHashSet<string> myInputElements = new JetHashSet<string>();

        public void SelectElement(string name)
        {
            myInputElements.Add(name);
        }

        public void Clear()
        {
            myInputElements.Clear();
        }

        public void Populate(IGeneratorContext context)
        {
            context.InputElements.AddRange(context.ProvidedElements.OfType<GeneratorDeclaredElement>()
                .Where(e => myInputElements.Contains(e.DeclaredElement.ShortName)));
        }

        // Must be greater than GenerateUnityEventFunctionsProvider.Priority
        public double Priority => 1000;
    }
}