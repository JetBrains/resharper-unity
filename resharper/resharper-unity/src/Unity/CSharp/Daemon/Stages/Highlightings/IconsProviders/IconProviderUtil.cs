using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    public class IconProviderUtil
    {
        public static bool ShouldShowGutterMarkIcon(IContextBoundSettingsStoreLive settings)
        {
            return settings.GetValue((UnitySettings key) => key.GutterIconMode) != GutterIconMode.None;
        }
    }
}