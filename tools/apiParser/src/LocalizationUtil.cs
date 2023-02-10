using System;

namespace ApiParser
{
    public static class LocalizationUtil
    {
        public static RiderSupportedLanguages TranslateCountryCodeIntoLanguageCode(string countryCode)
        {
            if (!UnityLanguages.TryParse<UnityLanguages>(countryCode, out var unityLang))
                throw new Exception($"Unsupported Unity country code {countryCode}");
            
            var index = (int)unityLang;
            return (RiderSupportedLanguages)Enum.ToObject(typeof(RiderSupportedLanguages), index);
        }
        

        public static string GetMessagesDivTextByLangCode(RiderSupportedLanguages langCode)
        {
            if (langCode == RiderSupportedLanguages.iv)
            {
                return "Messages";
            }
            else if (langCode == RiderSupportedLanguages.ja)
            {
                return "メッセージ";
            }
            else if (langCode == RiderSupportedLanguages.ko)
            {
                return "메시지";
            }
            else if (langCode == RiderSupportedLanguages.zh)
            {
                return "消息";
            }

            throw new Exception($"Unexpected lang code {langCode}");
        }

        public static string GetParametersDivTextByLangCode(RiderSupportedLanguages langCode)
        {
            if (langCode == RiderSupportedLanguages.iv)
                return "Parameters";
            else if (langCode == RiderSupportedLanguages.ja)
            {
                return "パラメーター";
            }
            else if (langCode == RiderSupportedLanguages.ko)
            {
                return "파라미터";
            }
            else if (langCode == RiderSupportedLanguages.zh)
            {
                return "参数";
            }
            
            throw new Exception($"Unexpected lang code {langCode}");
        }
    }

    // same enums in JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Help.LangCodeMap
    
    public enum UnityLanguages
    {
        en, ja, kr, cn
    }

    public enum RiderSupportedLanguages
    {
        iv, ja, ko, zh
    }
} 