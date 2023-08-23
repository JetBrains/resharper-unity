using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.BuildScript.Application.Zones;
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
                if (myVersion == UnityVersion.Unity54)
                    throw new InvalidOperationException("Network libs not available for Unity 5.4");
                if ((int) myVersion > (int) UnityVersion.Unity2018_4)
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
                    case UnityVersion.Unity2018_4: return "UNITY_2018_4";
                    case UnityVersion.Unity2019_1: return "UNITY_2019_1";
                    case UnityVersion.Unity2019_2: return "UNITY_2019_2";
                    case UnityVersion.Unity2019_3: return "UNITY_2019_3";
                    case UnityVersion.Unity2019_4: return "UNITY_2019_4";
                    case UnityVersion.Unity2020_1: return "UNITY_2020_1";
                    case UnityVersion.Unity2022_3: return "UNITY_2022_3";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private string ToVersionString(UnityVersion unityVersion)
        {
            switch (unityVersion)
            {
                // Note that the .0 here doesn't mean e.g. Unity 2018.1.0f1, it's just the package number. The actual
                // revision used is irrelevant, as we're interested in resolving the API, not in minor fixes
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
                case UnityVersion.Unity2018_4: return "2018.4.0";
                case UnityVersion.Unity2019_1: return "2019.1.0";
                case UnityVersion.Unity2019_2: return "2019.2.0";
                case UnityVersion.Unity2019_3: return "2019.3.0";
                case UnityVersion.Unity2019_4: return "2019.4.0";
                case UnityVersion.Unity2020_1: return "2020.1.0";
                case UnityVersion.Unity2022_3: return "2022.3.0";
                default:
                    throw new ArgumentOutOfRangeException(nameof(unityVersion), unityVersion, null);
            }
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