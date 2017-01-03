using System;
using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Properties.Flavours;
using JetBrains.ReSharper.TestFramework;
using NuGet;
using PlatformID = JetBrains.Application.platforms.PlatformID;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    public enum UnityVersion
    {
        Unity54,
        Unity55
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class TestUnityAttribute : TestPackagesAttribute, ITestFlavoursProvider, ITestPlatformProvider, ITestFileExtensionProvider
    {
        private readonly UnityVersion myVersion;

        public TestUnityAttribute() : this(UnityVersion.Unity54)
        {
        }

        public TestUnityAttribute(UnityVersion version)
        {
            myVersion = version;
        }

        public override IEnumerable<PackageDependency> GetPackages(PlatformID platformID)
        {
            // There isn't an official nuget for Unity, sadly, so add this feed to test/data/nuget.config
            // <add key="unity-testlibs" value="https://myget.org/F/resharper-unity/api/v2/" />
            switch (myVersion)
            {
                case UnityVersion.Unity54:
                    yield return ParsePackageDependency("resharper-unity.testlibs/5.4.0");
                    break;
                case UnityVersion.Unity55:
                    yield return ParsePackageDependency("resharper-unity.testlibs/5.5.0");
                    break;
            }
            foreach (var package in base.GetPackages(platformID))
                yield return package;
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

        public string Extension => CSharpProjectFileType.CS_EXTENSION;

        public string DefineConstants
        {
            get
            {
                switch (myVersion)
                {
                    case UnityVersion.Unity54: return "UNITY_5_4";
                    case UnityVersion.Unity55: return "UNITY_5_5";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}