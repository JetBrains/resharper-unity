using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ImportType;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.ImportType
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
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
            // Remove items from auto completion. Anything in these namespaces, or matching the method name will not
            // show in the auto import completion lists, and will not get the auto-import alt+enter tooltip popup.
            // If you want to use a type that matches, you need to add the using statement manually.
            // * Ignore Bool.Lang.* and UnityScript.* as they were deprecated in late 2017, and most of the code there
            //   is not intended to be consumed by users, but the assemblies are still referenced. This prevents e.g.:
            //   Boo.Lang.List`1 from appearing in the completion list above System.Collections.Generic.List`1
            // * Also exclude System.Diagnostics.Debug() to prevent similar clashes with UnityEngine.Debug()

            // See the internal DumpDuplicateTypeNamesAction to generate a list of types that can clash. There are about
            // 80 types on this list. It's not worth ignoring all of them. But some candidates:
            // * System.Numerics.Vector{2,3,4} + Quaternion + Plane
            // * System.Random + UnityEngine.Random
            AddAutoImportExclusions(mountPoint, "Boo.Lang.*", "UnityScript.*", "System.Diagnostics.Debug");
        }

        private void AddAutoImportExclusions(ISettingsStorageMountPoint mountPoint, params string[] settings)
        {
            var entry = mySettingsSchema.GetIndexedEntry((AutoImport2Settings s) => s.BlackLists);
            var indexedKey = new Dictionary<SettingsKey, object>
            {
                {mySettingsSchema.GetKey<AutoImport2Settings>(), CSharpLanguage.Name}
            };
            foreach (var setting in settings)
                ScalarSettingsStoreAccess.SetIndexedValue(mountPoint, entry, setting, indexedKey, true, null, myLogger);
        }
    }
}