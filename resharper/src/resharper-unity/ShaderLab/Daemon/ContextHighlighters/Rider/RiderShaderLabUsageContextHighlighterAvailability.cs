#if RIDER

using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.ContextHighlighters
{
    [ShellComponent]
    public class RiderShaderLabUsageContextHighlighterAvailability : ShaderLabUsageContextHighlighterAvailability
    {
        private readonly IRdProperty<bool> myIsAvailable;

        public RiderShaderLabUsageContextHighlighterAvailability([NotNull] SettingsModel settingsModel)
        {
            myIsAvailable = settingsModel.HighlightElementUnderCursor;
        }

        public override bool IsAvailable(IPsiSourceFile psiSourceFile)
        {
#pragma warning disable 618
            return Shell.Instance.IsTestShell ||
                (myIsAvailable.HasValue() && myIsAvailable.Value);
#pragma warning restore 618
        }
    }
}

#endif