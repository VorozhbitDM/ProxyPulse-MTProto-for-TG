using System;
using System.Linq;
using System.Text;

namespace ProxyPulse.Services
{
    /// <summary>Минимальная проверка secret (как tgnet decodeSecret) перед TCP.</summary>
    internal static class MtProxySecret
    {
        public static bool IsValid(string secretHexOrB64)
        {
            return Parse(secretHexOrB64) != null;
        }

        public static byte[] Parse(string secretHexOrB64)
        {
            var bytes = TelegramSecret.Decode(secretHexOrB64);
            if (bytes == null || bytes.Length == 0)
                return null;

            if (bytes.Length == 16)
                return bytes;

            if (bytes.Length == 17 && (bytes[0] == 0xDD || bytes[0] == 0xEE))
                return bytes.Skip(1).Take(16).ToArray();

            if (bytes.Length >= 18 && bytes[0] == 0xEE)
            {
                if (bytes.Length <= 17)
                    return null;
                return bytes.Skip(1).Take(16).ToArray();
            }

            return null;
        }

        private static class TelegramSecret
        {
            public static byte[] Decode(string input)
            {
                if (string.IsNullOrWhiteSpace(input))
                    return null;

                var trimmed = input.Trim();
                if (IsHex(trimmed) && trimmed.Length % 2 == 0)
                {
                    var bytes = new byte[trimmed.Length / 2];
                    for (var i = 0; i < bytes.Length; i++)
                        bytes[i] = Convert.ToByte(trimmed.Substring(i * 2, 2), 16);
                    return bytes;
                }

                return Base64UrlDecode(trimmed);
            }

            private static bool IsHex(string s)
            {
                for (var i = 0; i < s.Length; i++)
                {
                    var c = s[i];
                    if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
                        continue;
                    return false;
                }

                return true;
            }

            private static byte[] Base64UrlDecode(string value)
            {
                var s = value.TrimEnd('=');
                if (s.Length % 4 == 1)
                    return null;

                var mapped = new StringBuilder(s.Length);
                foreach (var c in s)
                {
                    if (c == '-')
                        mapped.Append('+');
                    else if (c == '_')
                        mapped.Append('/');
                    else
                        mapped.Append(c);
                }

                try
                {
                    return Convert.FromBase64String(mapped.ToString());
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
