using System;

namespace ApiParser
{
    public static class LocalizationUtil
    {
        public static RiderSupportedLanguages TranslateCountryCodeIntoLanguageCode(string countryCode)
        {
            if (!UnityLanguages.TryParse<UnityLanguages>(countryCode, out var unityLang))
                throw new Exception($"Unsupported Unity country code {countryCode}");
            
            if (unityLang == UnityLanguages.en)
            {
                return RiderSupportedLanguages.iv; // invariant lang
            }
            else if (unityLang == UnityLanguages.ja)
            {
                return RiderSupportedLanguages.ja;
            }
            else if (unityLang == UnityLanguages.kr)
            {
                return RiderSupportedLanguages.ko;
            }
            else if (unityLang == UnityLanguages.cn)
            {
                return RiderSupportedLanguages.zh;
            }

            throw new Exception($"Unexpected code {countryCode}");
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
    }

    public enum UnityLanguages
    {
        en, ja, kr, cn
    }

    public enum RiderSupportedLanguages
    {
        iv, ja, ko, zh
    }
} 