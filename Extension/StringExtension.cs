using System;
using System.Collections.Generic;
using System.Text;

namespace FtpManager.Extension
{
    public static class StringExtension
    {
        public static bool IsNullOrEmpty (this string text)
        {
            return string.IsNullOrEmpty(text);
        }
        public static string GetHtmlText(this string text)
        {
            return text.Replace(" ", "%20").Trim();
        }
    }
}
