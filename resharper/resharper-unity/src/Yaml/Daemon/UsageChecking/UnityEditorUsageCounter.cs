using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Daemon.UsageChecking
{
    [ShellComponent]
    public class UnityEditorUsageCounter : UsageCounterBase
    {
        public static readonly int Id = nameof(UnityEditorUsageCounter).GetPlatformIndependentHashCode();

        public UnityEditorUsageCounter() : base(Id)
        {
            
        }
    }
}