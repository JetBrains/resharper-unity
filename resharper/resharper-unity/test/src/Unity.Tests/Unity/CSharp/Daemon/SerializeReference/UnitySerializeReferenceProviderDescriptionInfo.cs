using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.SerializeReference
{
    internal static class UnitySerializeReferenceProviderDescriptionInfo
    {
        private static readonly ILogger ourLogger = Logger.GetLogger<UnitySerializedReferenceProvider>();

        public static void CreateLifetimeCookie(Lifetime testLifetime)
        {
            if(!ourLogger.IsTraceEnabled())
                Logger.IncreaseCategoriesLevel(testLifetime, LoggingLevel.TRACE, ourLogger.Category);
        }
    }
}