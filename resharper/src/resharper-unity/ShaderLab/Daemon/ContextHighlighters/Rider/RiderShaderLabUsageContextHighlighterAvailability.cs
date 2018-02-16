#if RIDER

using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ReSharper.Psi;
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
            return myIsAvailable.HasValue() && myIsAvailable.Value;
        }
    }
}

#endif