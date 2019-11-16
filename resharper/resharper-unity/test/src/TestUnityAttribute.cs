using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.platforms;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Assemblies.Impl;
using JetBrains.ProjectModel.ProjectImplementation;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Properties.Flavours;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Utils;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using PackageDependency = NuGet.Packaging.Core.PackageDependency;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    // ReSharper disable InconsistentNaming
    public enum UnityVersion
    {
        Unity54,
        Unity55,
        Unity56,
        Unity2017_1,
        Unity2017_2,
        Unity2017_3,
        Unity2017_4,
        Unity2018_1,
        Unity2018_2,
        Unity2018_3,

        DefaultTestVersion = Unity2018_3
    }
    // ReSharper restore InconsistentNaming

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class TestUnityAttribute : TestAspectAttribute, ITestLibraryReferencesProvider, ITestPackagesProvider,
        ITestFlavoursProvider, ITestPlatformProvider, ITestFileExtensionProvider, ICustomProjectPropertyAttribute
    {
        private readonly UnityVersion myVersion;

        public TestUnityAttribute()
            : this(UnityVersion.DefaultTestVersion)
        {
        }

        public TestUnityAttribute(UnityVersion version)
        {
            myVersion = version;
        }

        public bool IncludeNetworking { get; set; }

        public TargetFrameworkId GetTargetFrameworkId()
        {
            return TargetFrameworkId.Create(FrameworkIdentifier.NetFramework, new Version(4, 0));
        }

        public IEnumerable<PackageDependency> GetPackages(TargetFrameworkId targetFrameworkId)
        {
            // There isn't an official nuget for Unity, sadly, so add this feed to test/data/nuget.config
            // <add key="unity-testlibs" value="https://myget.org/F/resharper-unity/api/v2/" />
            return from name in GetPackageNames()
                select TestPackagesAttribute.ParsePackageDependency(name);
        }

        private IEnumerable<string> GetPackageNames()
        {
            var version = ToVersionString(myVersion);
            yield return $"resharper-unity.testlibs/{version}";
            if (IncludeNetworking)
            {
                if (myVersion == UnityVersion.Unity54)
                    throw new InvalidOperationException("Network libs not available for Unity 5.4");
                yield return $"resharper-unity.testlibs.networking/{version}";
            }
        }

        public Guid[] GetProjectTypeGuids()
        {
            return new[] {UnityProjectFlavor.UnityProjectFlavorGuid};
        }

        public IEnumerable<string> GetReferences(TargetFrameworkId targetFrameworkId, FileSystemPath testDataPath,
            NugetPackagesCache nugetPackagesCache)
        {
            var names = GetPackageNames().ToArray();
            var attribute = new TestPackagesAttribute(names);
            return attribute.GetReferences(targetFrameworkId, testDataPath, nugetPackagesCache);
        }

        public bool Inherits => false;
        public string Extension => CSharpProjectFileType.CS_EXTENSION;

        public string DefineConstants
        {
            get
            {
                switch (myVersion)
                {
                    case UnityVersion.Unity54: return "UNITY_5_4";
                    case UnityVersion.Unity55: return "UNITY_5_5";
                    case UnityVersion.Unity56: return "UNITY_5_6";
                    case UnityVersion.Unity2017_1: return "UNITY_2017_1";
                    case UnityVersion.Unity2017_2: return "UNITY_2017_2";
                    case UnityVersion.Unity2017_3: return "UNITY_2017_3";
                    case UnityVersion.Unity2017_4: return "UNITY_2017_4";
                    case UnityVersion.Unity2018_1: return "UNITY_2018_1";
                    case UnityVersion.Unity2018_2: return "UNITY_2018_2";
                    case UnityVersion.Unity2018_3: return "UNITY_2018_3";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private string ToVersionString(UnityVersion unityVersion)
        {
            switch (unityVersion)
            {
                case UnityVersion.Unity54: return "5.4.0";
                case UnityVersion.Unity55: return "5.5.0";
                case UnityVersion.Unity56: return "5.6.0";
                case UnityVersion.Unity2017_1: return "2017.1.0";
                case UnityVersion.Unity2017_2: return "2017.2.0";
                case UnityVersion.Unity2017_3: return "2017.3.0";
                case UnityVersion.Unity2017_4: return "2017.4.0";
                case UnityVersion.Unity2018_1: return "2018.1.0";
                case UnityVersion.Unity2018_2: return "2018.2.0";
                case UnityVersion.Unity2018_3: return "2018.3.0";
                default:
                    throw new ArgumentOutOfRangeException(nameof(unityVersion), unityVersion, null);
            }
        }

        public string PropertyName => "DefineConstants";
        public string PropertyValue => DefineConstants;

        // If we're not using the default test version, make sure we clean out the old Unity references. Because all
        // versions have an assembly version of 0.0, the test framework doesn't see that they're different, and they'll
        // get reused when they shouldn't
        protected override void OnAfterTestExecute(TestAspectContext context)
        {
            if (myVersion != UnityVersion.DefaultTestVersion)
            {
                var solution = context.TestProject.GetSolution();
                var targetFrameworkScopes = solution.GetComponent<ResolveContextManager>().EnumerateAllScopes();

                using (WriteLockCookie.Create())
                {
                    foreach (var targetFrameworkScope in targetFrameworkScopes)
                    {
                        if (targetFrameworkScope is ProjectTargetFrameworkScope projectScope)
                        {
                            // These methods are marked as obsolete to make sure they're only used from tests. We have
                            // warnings as errors on, so disable the warning
#pragma warning disable 618
                            projectScope.RemoveAllProjectReferences();
                            projectScope.RemoveAllResolveResults();
#pragma warning restore 618
                        }
                    }
                }
            }
        }
    }
}