#if RIDER
using JetBrains.Application;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    [ShellComponent]
    public class TestRdDispatcher : RdDispatcher
    {
        public TestRdDispatcher(IShellLocks shellLocks)
            : base(shellLocks)
        {
        }
    }
}
#endif