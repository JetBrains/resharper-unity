// Note(k15tfu): Can't use any of System.Runtime.InteropServices.RuntimeInformation in plugins sources
// because of the ambiguity between its ref assembly and mscorlib when building ourselves (e.g. SDK Mini),
// .NET SDK on the contrary ignores the ones from FrameworkList.xml and implicitly takes them from Microsoft.NETFramework.ReferenceAssemblies
// or `%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework` directory.  External aliases not available here.

namespace System.Runtime.InteropServices
{
    internal enum Architecture
    {
        X86,
        X64,
        Arm64
    }

    internal readonly struct OSPlatform
    {
        internal JetBrains.HabitatDetector.JetPlatform Platform { init; get; }

        internal static OSPlatform Linux => new() { Platform = JetBrains.HabitatDetector.JetPlatform.Linux };
        internal static OSPlatform OSX => new() { Platform = JetBrains.HabitatDetector.JetPlatform.MacOsX };
        internal static OSPlatform Windows => new() { Platform = JetBrains.HabitatDetector.JetPlatform.Windows };
    }

    internal static class RuntimeInformation
    {
        internal static Architecture ProcessArchitecture => JetBrains.HabitatDetector.HabitatInfo.ProcessArchitecture.ToArchitecture();

        internal static bool IsOSPlatform(OSPlatform osPlatform)
        {
            return JetBrains.HabitatDetector.HabitatInfo.Platform == osPlatform.Platform;
        }

        private static Architecture ToArchitecture(this JetBrains.HabitatDetector.JetArchitecture architecture)
        {
            return architecture switch
            {
                JetBrains.HabitatDetector.JetArchitecture.X86 => Architecture.X86,
                JetBrains.HabitatDetector.JetArchitecture.X64 => Architecture.X64,
                JetBrains.HabitatDetector.JetArchitecture.Arm64 => Architecture.Arm64,
                _ => throw new ArgumentOutOfRangeException(nameof(architecture), architecture, null)
            };
        }
    }
}