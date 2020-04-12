using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Host.Features.Settings.Layers.ExportImportWorkaround;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [ShellComponent]
    public class UnitySettingsCategoryProvider : IExportableSettingsCategoryProvider
    {
        private readonly Dictionary<string, Type[]> myCategoryToKeys = new Dictionary<string, Type[]>
        {
            {"Unity plugin settings", new [] { typeof(UnitySettings) }}
        };

        public bool TryGetRelatedIdeaConfigsBy(string category, out string[] configs)
        {
            configs = default(string[]);
            return false;
        }

        public bool TryGetCategoryBy(SettingsKey settingsKey, out string category)
        {
            category = null;
            foreach (var pair in myCategoryToKeys)
            {
                foreach (var type in pair.Value)
                {
                    if (settingsKey.SettingsKeyClassClrType.Equals(type))
                    {
                        category = pair.Key;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}