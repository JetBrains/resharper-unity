using System;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef
{
    public static class AsmDefUtils
    {
        public static string FormatGuidReference(Guid guid) => $"guid:{guid:N}";
    }
}