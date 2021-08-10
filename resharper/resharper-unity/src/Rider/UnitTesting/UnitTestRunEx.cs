using System.Collections.Generic;
using JetBrains.Application.Components;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.ReSharper.UnitTestFramework.Launch;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    public static class UnitTestRunEx
    {
        public static void AddDynamicElement(this IUnitTestRun run, IUnitTestElement element)
        {
            using (ReadLockCookie.Create())
            {
                run.CreateDynamicElement(() => element);
            }
        }
    }
}