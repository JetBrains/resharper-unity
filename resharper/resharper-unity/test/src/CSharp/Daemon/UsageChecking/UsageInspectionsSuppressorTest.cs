using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Tests.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.UsageChecking
{
    [TestUnity]
    public class UsageInspectionsSuppressorTest : UsageCheckBaseTest
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\UsageChecking";
        private Action<IProject> myOnProjectStarted;
        private Action<IProject> myOnProjectFinished;

        [Test] public void MonoBehaviourMethods01() { DoNamedTest(); }
        [Test] public void MonoBehaviourFields01() { DoNamedTest(); }
        [Test] public void SerializableClassFields01() { DoNamedTest(); }

        protected override void DoTest(IProject project)
        {
            myOnProjectStarted?.Invoke(project);
            try
            {
                base.DoTest(project);
            }
            finally
            {
                myOnProjectFinished?.Invoke(project);
            }

            myOnProjectStarted = myOnProjectFinished = null;
        }

        [Test]
        public void PotentialEventHandlerMethodsYamlDisabled()
        {
            // TODO: Support testing when YAML is enabled
            // It's currently enabled by default, but nothing is processed, because the PSI module is disabled to work
            // around a R# issue
            var oldValue = false;
            myOnProjectStarted = project =>
            {
                var yamlSupport = project.GetSolution().GetComponent<UnityYamlSupport>();
                oldValue = yamlSupport.IsUnityYamlParsingEnabled.Value;
                yamlSupport.IsUnityYamlParsingEnabled.Value = false;
            };

            myOnProjectFinished = project =>
            {
                var yamlSupport = project.GetSolution().GetComponent<UnityYamlSupport>();
                yamlSupport.IsUnityYamlParsingEnabled.Value = oldValue;
            };

            DoNamedTest();
        }

        [Test]
        public void PotentialEventHandlerMethodsSerializationNotText()
        {
            var oldMode = AssetSerializationMode.SerializationMode.Unknown;
            myOnProjectStarted = project =>
            {
                var assetSerializationMode = Solution.GetComponent<TestableAssetSerializationMode>();
                oldMode = assetSerializationMode.SetMode(AssetSerializationMode.SerializationMode.Mixed);
            };

            myOnProjectFinished = project =>
            {
                var assetSerializationMode = Solution.GetComponent<TestableAssetSerializationMode>();
                assetSerializationMode.SetMode(oldMode);
            };

            DoNamedTest();
        }
    }
}