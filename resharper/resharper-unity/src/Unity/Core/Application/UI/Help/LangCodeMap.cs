#nullable enable
using System;
using System.Globalization;
using JetBrains.Diagnostics;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Help
{
    public class LangCodeMap
    {
        // Attention: same enums in ApiParser.LocalizationUtil
        
        public enum UnityLanguages
        {
            en, ja, kr, cn
        }

        public enum RiderSupportedLanguages
        {
            iv, ja, ko, zh
        }

        private static UnityLanguages ToUnityLanguage(string langCode, ILog logger)
        {
            if (!Enum.TryParse<RiderSupportedLanguages>(langCode, out var riderSupportedLanguages))
            {
                logger.Warn($"Failed to parse langCode {langCode}");
                return UnityLanguages.en;
            }
            
            var index = (int)riderSupportedLanguages;
            return (UnityLanguages)Enum.ToObject(typeof(UnityLanguages), index);
        }
        
        public static string GetUnityLangCode(CultureInfo culture, ILog logger)
        {
            return ToUnityLanguage(culture.TwoLetterISOLanguageName, logger).ToString();
        }
    }
}