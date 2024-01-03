using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Tests.UnityTestComponents;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.Unity
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CgIncludesDirectoryAttribute : TestAspectAttribute
    {
        public string CgIncludesPath { get; }

        public CgIncludesDirectoryAttribute(string cgIncludesPath)
        {
            CgIncludesPath = cgIncludesPath;
        }

        protected override void OnBeforeTestExecute(TestAspectContext context)
        {
            var cgIncludesFolderPath = context.TestFixture.BaseTestDataPath.Combine(CgIncludesPath);
            var cgIncludeDirectoryProvider = context.TestProject.GetSolution().GetComponent<CgIncludeDirectoryProviderStub>();
            var savedPath = cgIncludeDirectoryProvider.CgIncludeFolderPathOverride;
            cgIncludeDirectoryProvider.CgIncludeFolderPathOverride = cgIncludesFolderPath.ToVirtualFileSystemPath();
            context.TestLifetime.OnTermination(() => { cgIncludeDirectoryProvider.CgIncludeFolderPathOverride = savedPath; });
        }
    }
}