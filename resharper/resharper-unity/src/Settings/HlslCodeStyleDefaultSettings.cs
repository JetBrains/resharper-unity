using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeStyle;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
{
    [SolutionComponent]
    public class HlslCodeStyleDefaultSettings : IUnitySolutionSettingsProvider
    {
        private readonly ISettingsSchema mySettingsSchema;
        private readonly ILogger myLogger;

        public HlslCodeStyleDefaultSettings(ISettingsSchema settingsSchema, ILogger logger)
        {
            mySettingsSchema = settingsSchema;
            myLogger = logger;
        }

        public void InitialiseSolutionSettings(ISettingsStorageMountPoint mountPoint)
        {
            var entry = mySettingsSchema.GetScalarEntry((CppFormattingSettingsKey s) => s.INDENT_PREPROCESSOR_DIRECTIVES);
            ScalarSettingsStoreAccess.SetValue(mountPoint, entry, null, PreprocessorIndent.Normal, true, null, myLogger);
        }
    }
}