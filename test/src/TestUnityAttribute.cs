using System;
using JetBrains.Application.platforms;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Properties.Flavours;
using JetBrains.ReSharper.TestFramework;
using PlatformID = JetBrains.Application.platforms.PlatformID;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class TestUnityAttribute : TestPackagesAttribute, ITestFlavoursProvider, ITestPlatformProvider
    {
        public TestUnityAttribute(): base(GetReferences())
        {
        }

        private static string[] GetReferences()
        {
            var rootPath = PlatformUtils.GetProgramFiles()/@"Unity/Editor/Data/Managed";
            return new[]
            {
                (rootPath/"UnityEngine.dll").FullPath,
                (rootPath/"UnityEditor.dll").FullPath
            };
        }

        public Guid[] GetProjectTypeGuids()
        {
            return new[]
            {
                UnityProjectFlavor.UnityProjectFlavorGuid
            };
        }

        public PlatformID GetPlatformID()
        {
            return PlatformID.CreateFromName(".NETFrameWork", new Version(4, 0));
        }
    }
}