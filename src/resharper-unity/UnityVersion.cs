using System;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class UnityVersion
    {
        public UnityVersion()
        {
            // TODO: Get proper version
            // This is tricky, though. UnityEngine and UnityEditor are unversioned
            // We could read the version from defines, but that's not a guarantee
            Version = new Version(5, 4);

            // TODO: Uncomment VersionSpecificCompletionListTest.OnParticleTriggerWithNoArgs55
        }

        public Version Version { get; }
    }
}