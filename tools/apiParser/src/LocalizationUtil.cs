using System;

namespace ApiParser
{
    public static class LocalizationUtil
    {
        public static string GetMessagesDivTextByLangCode(string langCode)
        {
            if (langCode.Equals("en"))
            {
                return "Messages";
            }
            else if (langCode.Equals("ja"))
            {
                return "メッセージ";
            }
            else if (langCode.Equals("kr"))
            {
                return "메시지";
            }
            else if (langCode.Equals("cn"))
            {
                return "消息";
            }

            throw new Exception($"Unexpected lang code {langCode}");
        }
    }
} 