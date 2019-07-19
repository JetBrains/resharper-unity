using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ImportType.BlackList;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [SolutionComponent]
    public class AutoImportSolutionSettingsProvider : IUnitySolutionSettingsProvider
    {
        private readonly ISettingsSchema mySettingsSchema;
        private readonly ILogger myLogger;

        public AutoImportSolutionSettingsProvider(ISettingsSchema settingsSchema, ILogger logger)
        {
            mySettingsSchema = settingsSchema;
            myLogger = logger;
        }

        public void InitialiseSolutionSettings(ISettingsStorageMountPoint mountPoint)
        {
            // Remove everything in the Boo.Lang.* and UnityScript.* namespaces from all auto completion lists. They've
            // been deprecated since late 2017, and most of the code here is not intended to be consumed by users. It's
            // also really annoying that Boo.Lang.List`1 appears before System.Collections.Generic.List`1
            // This means types won't show in import completion lists, the auto-import popup or any other quick fixes.
            // If you want to use Boo.Lang.List`1, you'll need to add the using statement manually
            //
            // NOTE: This settings format is likely to change, as this overwrites any settings in the global layer.
            // See RIDER-30397
            SetIndexedValue(mountPoint, (AutoImportSettings s) => s.BlackLists, CSharpLanguage.Name,
                "Boo.Lang.*;UnityScript.*");
        }

        private void SetIndexedValue<TKeyClass, TEntryIndex, TEntryValue>([NotNull] ISettingsStorageMountPoint mount,
                                                                          [NotNull] Expression<Func<TKeyClass, IIndexedEntry<TEntryIndex, TEntryValue>>> lambdaexpression,
                                                                          [NotNull] TEntryIndex index,
                                                                          [NotNull] TEntryValue value,
                                                                          IDictionary<SettingsKey, object> keyIndices = null)
        {
            ScalarSettingsStoreAccess.SetIndexedValue(mount, mySettingsSchema.GetIndexedEntry(lambdaexpression), index,
                keyIndices, value, null, myLogger);
        }
    }
}