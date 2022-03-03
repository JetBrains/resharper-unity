using System;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef
{
    public static class AsmDefUtils
    {
        // Unity serialises with uppercase "GUID:", but will read either case. The GUID itself is formatted in lowercase
        public static string FormatGuidReference(Guid guid) => $"GUID:{guid:N}";

        public static bool IsGuidReference(string reference) =>
            reference.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase);

        public static bool TryParseGuidReference(string reference, out Guid guid)
        {
            guid = Guid.Empty;
            // Skip the 5 chars of "GUID:"
            return IsGuidReference(reference) && Guid.TryParse(reference[5..], out guid);
        }
    }
}