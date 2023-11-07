using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Application.platforms;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Assemblies.Impl;
using JetBrains.ProjectModel.ProjectImplementation;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Propoerties;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.Properties.Flavours;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.JetNuGet;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NuGet.Packaging.Core;

namespace JetBrains.ReSharper.Plugins.Tests.Unity
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
        Unity2018_4,
        Unity2019_1,
        Unity2019_2,
        Unity2019_3,
        Unity2019_4,
        Unity2020_1,
        Unity2022_3,

        // General rule: Keep the default version at the latest LTS Unity version
        // If you need a newer/specific version for a specific test, use [TestUnity(UnityVersion.Unity2020_1)], etc.
        DefaultTestVersion = Unity2019_4
    }
    // ReSharper restore InconsistentNaming

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class TestUnityAttribute : TestAspectAttribute, ITestLibraryReferencesProvider, ITestPackagesProvider,
        ITestFlavoursProvider, ITestTargetFrameworkIdProvider, ITestFileExtensionProvider, ICustomProjectPropertyAttribute
    {
        private static readonly Version ourDefaultVersion = ToVersion(UnityVersion.DefaultTestVersion);
        private static readonly Version ourMinNetworkingVersion = new(5, 5);
        private static readonly Version ourMaxNetworkingVersion = new(2018, 4, int.MaxValue);
        
        private readonly Version myVersion;

        public TestUnityAttribute() : this(ourDefaultVersion) { }

        public TestUnityAttribute(UnityVersion version) : this(ToVersion(version)) { }
        public TestUnityAttribute(int major, int minor) : this(new Version(major, minor)) { }
        public TestUnityAttribute(int major, int minor, int build) : this(new Version(major, minor, build)) { }

        private TestUnityAttribute(Version version)
        {
            myVersion = version;
        }

        public bool ProvideReferences { get; set; } = true;
        public bool IncludeNetworking { get; set; }

        public TargetFrameworkId GetTargetFrameworkId()
        {
            return TargetFrameworkId.Create(FrameworkIdentifier.NetFramework, new Version(4, 0));
        }

        public IEnumerable<PackageDependency> GetPackages(TargetFrameworkId? targetFrameworkId)
        {
            // unlisted on nuget.org
            return from name in GetPackageNames()
                select TestPackagesAttribute.ParsePackageDependency(name);
        }

        private IEnumerable<string> GetPackageNames()
        {
            var version = ToVersionString(myVersion);
            yield return $"JetBrains.Resharper.Unity.TestDataLibs/{version}";
            if (IncludeNetworking)
            {
                if (myVersion < ourMinNetworkingVersion)
                    throw new InvalidOperationException("Network libs not available for Unity 5.4");
                if (myVersion > ourMaxNetworkingVersion)
                    throw new InvalidOperationException("Network libs no longer supported in Unity 2019.1+");
                yield return $"JetBrains.Resharper.Unity.TestDataLibs.Networking/{version}";
            }
        }

        public Guid[] GetProjectTypeGuids()
        {
            return new[] {UnityProjectFlavor.UnityProjectFlavorGuid};
        }

        public IEnumerable<string> GetReferences(BaseTestNoShell test, TargetFrameworkId targetFrameworkId,
            FileSystemPath testDataPath, NuGetPackageCache nugetPackagesCache)
        {
            if (!ProvideReferences)
                return EmptyList<string>.Enumerable;
            var names = GetPackageNames().ToArray();
            var attribute = new TestPackagesAttribute(names);
            return attribute.GetReferences(test, targetFrameworkId, testDataPath, nugetPackagesCache);
        }

        public bool Inherits => false;
        public string Extension => CSharpProjectFileType.CS_EXTENSION;

        public string DefineConstants
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append($"UNITY_{myVersion.Major};UNITY_{myVersion.Major}_{myVersion.Minor}");
                if (myVersion.Build >= 0)
                    sb.Append($";UNITY_{myVersion.Major}_{myVersion.Minor}_{myVersion.Build}");
                return sb.ToString();
            }
        }

        private static Version ToVersion(UnityVersion version)
        {
            return version switch
            {
                UnityVersion.Unity54 => new Version(5, 4),
                UnityVersion.Unity55 => new Version(5, 5),
                UnityVersion.Unity56 => new Version(5, 6),
                UnityVersion.Unity2017_1 => new Version(2017, 1, 0),
                UnityVersion.Unity2017_2 => new Version(2017, 2, 0),
                UnityVersion.Unity2017_3 => new Version(2017, 3, 0),
                UnityVersion.Unity2017_4 => new Version(2017, 4, 0),
                UnityVersion.Unity2018_1 => new Version(2018, 1, 0),
                UnityVersion.Unity2018_2 => new Version(2018, 2, 0),
                UnityVersion.Unity2018_3 => new Version(2018, 3, 0),
                UnityVersion.Unity2018_4 => new Version(2018, 4, 0),
                UnityVersion.Unity2019_1 => new Version(2019, 1, 0),
                UnityVersion.Unity2019_2 => new Version(2019, 2, 0),
                UnityVersion.Unity2019_3 => new Version(2019, 3, 0),
                UnityVersion.Unity2019_4 => new Version(2019, 4, 0),
                UnityVersion.Unity2020_1 => new Version(2020, 1, 0),
                UnityVersion.Unity2022_3 => new Version(2022, 3, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
            };
        }

        private static string ToVersionString(Version version)
        {
            // Note that the .0 here doesn't mean e.g. Unity 2018.1.0f1, it's just the package number. The actual
            // revision used is irrelevant, as we're interested in resolving the API, not in minor fixes
            return $"{version.Major}.{version.Minor}.0";
        }

        public string PropertyName => "DefineConstants";
        public string PropertyValue => DefineConstants;

        protected override void OnBeforeTestExecute(TestAspectContext context)
        {
            foreach (var activeConfiguration in context.TestProject.ProjectProperties
                .GetActiveConfigurations<CSharpProjectConfiguration>())
            {
                var projectConfiguration = activeConfiguration;
                var oldCompilationSymbols = projectConfiguration.DefineConstants;
                if (string.IsNullOrEmpty(projectConfiguration.DefineConstants))
                    projectConfiguration.DefineConstants = DefineConstants;
                else
                {
                    // Remove any UNITY_ version defines already in there. This might happen if the attribute has been
                    // applied to a class, and then also to a method to override.
                    projectConfiguration.DefineConstants = string.Join(";",
                        projectConfiguration.DefineConstants.Split(';').Where(s => !s.StartsWith("UNITY_"))
                            .Concat(DefineConstants));
                }

                context.TestLifetime.OnTermination(() => projectConfiguration.DefineConstants = oldCompilationSymbols);
            }
        }

        // If we're not using the default test version, make sure we clean out the old Unity references. Because all
        // versions have an assembly version of 0.0, the test framework doesn't see that they're different, and they'll
        // get reused when they shouldn't
        protected override void OnAfterTestExecute(TestAspectContext context)
        {
            if (myVersion != ourDefaultVersion)
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