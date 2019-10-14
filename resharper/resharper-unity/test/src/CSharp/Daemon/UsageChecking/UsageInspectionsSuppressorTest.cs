using System;
using JetBrains.Lifetimes;
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
        [Test] public void PreprocessBuildInterface01() { DoNamedTest(); }
        [Test] public void SettingsProviderAttribute01() { DoNamedTest(); }

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