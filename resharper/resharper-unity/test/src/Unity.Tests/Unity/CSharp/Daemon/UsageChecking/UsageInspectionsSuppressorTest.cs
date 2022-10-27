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
        private Action<IProject> myOnProjectStarted;
        private Action<IProject> myOnProjectFinished;

        [Test] public void MonoBehaviourMethods01() { DoNamedTest(); }
        [Test] public void MonoBehaviourFields01() { DoNamedTest(); }
        [Test] public void SerializableClassFields01() { DoNamedTest(); }
        [Test] public void PreprocessBuildInterface01() { DoNamedTest(); }
        [Test] public void PreprocessBuildInterface02() { DoNamedTest(); }
        [Test] public void MethodWithAttributeWithRequiredSignature() { DoNamedTest(); }
        [Test] public void UnityEcsSystemClass() { DoNamedTest(); }
        [Test] public void UnityEcsSystemStruct() { DoNamedTest(); }

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
