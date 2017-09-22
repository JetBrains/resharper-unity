using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.LiveTemplates
{
    // Defines empty settings for the Unity QuickList, or we don't get a QuickList at all
    [ShellComponent]
    public class UnityQuickListDefaultSettings : HaveDefaultSettings
    {
        private readonly IMainScopePoint myMainPoint;

        public UnityQuickListDefaultSettings(ILogger logger, ISettingsSchema settingsSchema, UnityProjectScopeCategoryUIProvider provider)
            : base(logger, settingsSchema)
        {
            myMainPoint = provider.MainPoint;
        }

        public override void InitDefaultSettings(ISettingsStorageMountPoint mountPoint)
        {
            var settings = new QuickListSettings {Name = myMainPoint.QuickListTitle};
            SetIndexedKey(mountPoint, settings, new GuidIndex(myMainPoint.QuickListUID));
        }

        public override string Name => "Unity QuickList settings";
    }
}