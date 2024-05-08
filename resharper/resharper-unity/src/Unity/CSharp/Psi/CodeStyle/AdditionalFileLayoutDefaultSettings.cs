using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle
{
    // This doesn't affect any non-Unity projects, so it's a global default, and can be overridden safely in a user's
    // global settings
    [DefaultSettings(typeof(AdditionalFileLayoutSettings), Instantiation.DemandAnyThreadSafe)]
    public class AdditionalFileLayoutDefaultSettings : HaveDefaultSettings
    {
        public AdditionalFileLayoutDefaultSettings(ISettingsSchema settingsSchema, ILogger logger)
            : base(settingsSchema, logger)
        {
        }

        public override void InitDefaultSettings(ISettingsStorageMountPoint mountPoint)
        {
            var text = AdditionalFileLayoutResources.DefaultAdditionalFileLayoutPatterns;
            SetValue(mountPoint, (AdditionalFileLayoutSettings s) => s.Pattern, text);
        }

        public override string Name => "Unity Additional C# File Layout Patterns";
    }
}