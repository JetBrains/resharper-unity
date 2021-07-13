using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using HarmonyLib;
using JetBrains.Reflection;
using JetBrains.Util;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedParameter.Local

namespace JetBrains.ReSharper.Plugins.Yaml.Tests
{
    // Yes. This is horrible. Bite me.
    // A combination of NuGet 2.x and Mono's naive implementation of System.IO.Packaging destroys any chance of
    // performance for the TestDataInNugets class. This set of astonishing runtime patches fixes the main symptoms, and
    // gives us back normal performance, but at the price of my mortal soul.
    // TestDataInNugets is scheduled to be ported to NuGet 4+ at some point, at which point all of this is unnecessary.
    // This uses the Lib.Harmony package to patch methods at runtime. Dirty, but really, really effective.
    public static class HackTestDataInNugets
    {
        public static void ApplyPatches()
        {
            if (!PlatformUtil.IsRunningOnMono)
                return;

            var harmony = new Harmony("com.jetbrains.resharper.tests::nuget_packaging");

            // Mono's System.IO.Packaging has a very naive implementation of Package. ZipPackage.LoadParts is the most
            // expensive, usually doing two xpath lookups in [Content_Types].xml for each file. Our packages have e.g.
            // 574 files, and this takes a loooong time to complete. It also calls into a native library to get the
            // stream compression option, and we don't care about either of these things, so don't waste time
            // calculating them.
            var originalMethod = AccessTools.Method(typeof(ZipPackage), "LoadParts");
            var prefixMethod = new HarmonyMethod(AccessTools.Method(typeof(HackTestDataInNugets), nameof(New_System_IO_Packaging_ZipPackage_LoadParts)));
            harmony.Patch(originalMethod, prefixMethod);

            // Reverse patch System.IO.Packaging.ZipPackage.CreatePartCore onto HackTestDataInNugets.NuGet_ZipPacakge_CreatePartCore
            // to save us doing a lot of private reflection from our reimplementation of System.IO.Packaging.ZipPackage.LoadParts
            harmony.CreateReversePatcher(AccessTools.Method(typeof(ZipPackage), "CreatePartCore"),
                new HarmonyMethod(AccessTools.Method(typeof(HackTestDataInNugets), nameof(System_IO_Packaging_ZipPackage_CreatePartCore)))).Patch();
        }

        // Replaces ZipPackage.LoadParts, which is naive and very, very slow, and used every time a package is opened.
        private static bool New_System_IO_Packaging_ZipPackage_LoadParts(ZipPackage __instance, ref Dictionary<Uri, ZipPackagePart> ___parts)
        {
            ___parts = new Dictionary<Uri, ZipPackagePart>();
            var packageStream = (Stream)__instance.GetDynamicFieldOrProperty("PackageStream");
            using (var zipArchive = new ZipArchive(packageStream, ZipArchiveMode.Read, true))
            {
                foreach (var zipArchiveEntry in zipArchive.Entries)
                {
                    // We don't care what Packaging thinks the content type is
                    System_IO_Packaging_ZipPackage_CreatePartCore(__instance,
                        new Uri("/" + zipArchiveEntry.FullName, UriKind.Relative), "application/octet",
                        CompressionOption.Maximum);
                }
            }
            return false;
        }

        // Patched to act like a call to ZipPackage.CreatePartCore, but avoiding private reflection
        private static PackagePart System_IO_Packaging_ZipPackage_CreatePartCore(object instance, Uri partUri, string contentType, CompressionOption compressionOption)
        {
            throw new InvalidOperationException("Stub replaced at runtime");
        }
    }
}