using System;
using System.IO;
using JetBrains.HabitatDetector;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.Util;
using NUnit.Framework;
// ReSharper disable LocalizableElement

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.Integration
{
    // Explicit: these tests check UnityAssemblyScanner against real Unity player builds that are
    // only available on a developer's machine. Build mono/il2cpp/coreCLR standalone players for the
    // current platform and point the paths below at them to run this fixture locally.
    [TestFixture]
    [Explicit("Requires locally available Unity player builds; see paths in the comments above")]
    public class UnityAssemblyScannerTest
    {
        private const string MonoAppPath = "/Users/ivan.shakhov/Work/com.unity.ide.rider/mono.app";
        private const string Il2CppAppPath = "/Users/ivan.shakhov/Work/com.unity.ide.rider/il2cpp.app";
        private const string CoreClrAppPath = "/Users/ivan.shakhov/Work/com.unity.ide.rider/coreCLR.app";
        private const string LinuxMonoPlayerPath = "/Users/ivan.shakhov/Work/com.unity.ide.rider/linux_mono/linux-mono.x86_64";
        private const string LinuxIl2CppPlayerPath = "/Users/ivan.shakhov/Work/com.unity.ide.rider/linux_il2cpp/linux-il2cpp.x86_64";
        private const string LinuxCoreClrPlayerPath = "/Users/ivan.shakhov/Work/com.unity.ide.rider/linux_core/linux-core.x86_64";
        private const string WindowsMonoLibPath = "/Users/ivan.shakhov/Work/com.unity.ide.rider/windows-mono/UnityPlayer.dll";
        private const string WindowsCoreClrLibPath = "/Users/ivan.shakhov/Work/com.unity.ide.rider/windows-coreCLR/UnityPlayer.dll";
        private const string UnityExecutablePath = "/Applications/Unity/Hub/Editor/6000.7.0b2/Unity.app/Contents/MacOS/Unity";

        [Test]
        public void Scan_ReadsMonoBackend_FromUnityExecutablePath()
        {
            var result = UnityAssemblyScanner.TryScan(UnityExecutablePath).Result;
            Assert.AreEqual(UnityScriptingBackend.Mono, result);
        }

        [Test]
        public void Scan_ReadsMonoBackend_FromLocalBuild()
        {
            Assert.AreEqual(UnityScriptingBackend.Mono, Scan(MonoAppPath));
        }

        [Test]
        public void Scan_ReadsIl2CppBackend_FromLocalBuild()
        {
            Assert.AreEqual(UnityScriptingBackend.IL2CPP, Scan(Il2CppAppPath));
        }

        [Test]
        public void Scan_ReadsCoreClrBackend_FromLocalBuild()
        {
            Assert.AreEqual(UnityScriptingBackend.CoreCLR, Scan(CoreClrAppPath));
        }

        [Test]
        public void Scan_ReadsMonoBackend_FromLinuxBuild()
        {
            Assert.AreEqual(UnityScriptingBackend.Mono, ScanLinux(LinuxMonoPlayerPath));
        }

        [Test]
        public void Scan_ReadsIl2CppBackend_FromLinuxBuild()
        {
            Assert.AreEqual(UnityScriptingBackend.IL2CPP, ScanLinux(LinuxIl2CppPlayerPath));
        }

        [Test]
        public void Scan_ReadsCoreClrBackend_FromLinuxBuild()
        {
            Assert.AreEqual(UnityScriptingBackend.CoreCLR, ScanLinux(LinuxCoreClrPlayerPath));
        }

        [Test]
        public void Scan_ReadsMonoBackend_FromWindowsBuild()
        {
            Assert.AreEqual(UnityScriptingBackend.Mono, UnityAssemblyScanner.TryScan(WindowsMonoLibPath).Result);
        }

        [Test]
        public void Scan_ReadsCoreClrBackend_FromWindowsBuild()
        {
            Assert.AreEqual(UnityScriptingBackend.CoreCLR, UnityAssemblyScanner.TryScan(WindowsCoreClrLibPath).Result);
        }

        private static UnityScriptingBackend ScanLinux(string playerPath)
        {
            var libraryPath = Path.Combine(Path.GetDirectoryName(playerPath)!, "UnityPlayer.so");
            return UnityAssemblyScanner.TryScan(libraryPath).Result;
        }

        private static UnityScriptingBackend Scan(string appPath)
        {
            var libraryPath = ResolveUnityPlayerLib(appPath);
            return UnityAssemblyScanner.TryScan(libraryPath).Result;
        }

        // Ported from UnityPlayerRuntimeDetector.resolveUnityPlayerLib/resolveNativeLibraryDirectory/
        // nativeLibraryExtension (rider/src/main/kotlin/.../util/UnityPlayerRuntimeDetector.kt), so the
        // test locates the same `UnityPlayer` library that the frontend scans in production.
        private static string ResolveUnityPlayerLib(string appPath)
        {
            var extension = NativeLibraryExtension;
            var libraryDir = ResolveNativeLibraryDirectory(appPath);
            return Path.Combine(libraryDir, "UnityPlayer." + extension);
        }

        private static string ResolveNativeLibraryDirectory(string exePath)
        {
            if (PlatformUtil.RuntimePlatform == JetPlatform.MacOsX)
                return Path.Combine(FindAppBundle(exePath), "Contents", "Frameworks");

            return Path.GetDirectoryName(exePath) ?? exePath;
        }

        private static string NativeLibraryExtension => PlatformUtil.RuntimePlatform switch
        {
            JetPlatform.Windows => "dll",
            JetPlatform.MacOsX => "dylib",
            JetPlatform.Linux => "so",
            _ => throw new PlatformNotSupportedException(),
        };

        private static string FindAppBundle(string exePath)
        {
            for (var current = exePath; current != null; current = Path.GetDirectoryName(current))
            {
                if (current.EndsWith(".app"))
                    return current;
            }

            throw new ArgumentException($"No .app bundle found for '{exePath}'", nameof(exePath));
        }
    }
}
