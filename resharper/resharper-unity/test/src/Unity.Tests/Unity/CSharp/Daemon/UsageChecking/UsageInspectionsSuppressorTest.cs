using System;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Tests.TestFramework;
using JetBrains.ReSharper.Plugins.Tests.UnityTestComponents;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.UsageChecking
{
    // Require 2020.1 to test suppressing Dictionary<string, string> as a field
    // TODO: Create separate tests for serialisation logic
    [TestUnity(UnityVersion.Unity2020_1)]
    public class UsageInspectionsSuppressorTest : UsageCheckBaseTest
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\UsageChecking";
        private Action<IProject>? myOnProjectStarted;
        private Action<IProject>? myOnProjectFinished;

        [Test, UnityPluginBackendChecklist("C# code analysis", "Implicit usages", "Suppressed inspections for Unity members")] public void MonoBehaviourMethods01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Implicit usages", "Suppressed inspections for Unity members")] public void MonoBehaviourFields01() { DoNamedTest(); }
        [Test, Ignore("SerializableAttribute has MeansImplicitUseAttribute"), UnityPluginBackendChecklist("C# code analysis", "Implicit usages", "Suppressed inspections for Unity members")] public void SerializableClassFields01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Implicit usages", "Suppressed inspections for Unity members")] public void PreprocessBuildInterface01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Implicit usages", "Suppressed inspections for Unity members")] public void PreprocessBuildInterface02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Implicit usages", "Suppressed inspections for Unity members")] public void MethodWithAttributeWithRequiredSignature() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Implicit usages", "Suppressed inspections for Unity members")] public void UnityEcsSystemClass() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Implicit usages", "Suppressed inspections for Unity members")] public void UnityEcsSystemStruct() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Implicit usages", "Suppressed inspections for Unity members")] public void UnityDotsBacker() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Implicit usages", "Suppressed inspections for Unity members")] public void JobEntityRefParameter() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Implicit usages", "Suppressed inspections for Unity members")] public void DotsSequentialStruct() { DoNamedTest(); }

        protected override void DoTest(Lifetime lifetime, IProject project)
        {
            myOnProjectStarted?.Invoke(project);
            try
            {
                base.DoTest(lifetime, project);
            }
            finally
            {
                myOnProjectFinished?.Invoke(project);
            }

            myOnProjectStarted = myOnProjectFinished = null;
        }

        [Test]
        [UnityPluginBackendChecklist("C# code analysis", "Implicit usages", "Suppressed inspections for Unity members")]
        public void PotentialEventHandlerMethodsSerializationNotText()
        {
            var oldMode = AssetSerializationMode.SerializationMode.Unknown;
            myOnProjectStarted = _ =>
            {
                var assetSerializationMode = Solution.GetComponent<TestableAssetSerializationMode>();
                oldMode = assetSerializationMode.SetMode(AssetSerializationMode.SerializationMode.Mixed);
            };

            myOnProjectFinished = _ =>
            {
                var assetSerializationMode = Solution.GetComponent<TestableAssetSerializationMode>();
                assetSerializationMode.SetMode(oldMode);
            };

            DoNamedTest();
        }
    }
}
