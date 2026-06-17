using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.IdP.Services
{
    public class TotpService
    {
        private static readonly string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public string GenerateSecretKey()
        {
            var bytes = new byte[20];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return ToBase32String(bytes);
        }

        public bool VerifyTotp(string secret, string code, out string? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(code) || code.Length != 6 || !int.TryParse(code, out _))
            {
                error = "Invalid code format. It must be a 6-digit number.";
                return false;
            }

            byte[] secretBytes;
            try
            {
                secretBytes = FromBase32String(secret);
            }
            catch (Exception)
            {
                error = "Invalid TOTP secret configuration.";
                return false;
            }

            long currentUnixTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            long step = currentUnixTime / 30;

            // Check standard window -1, 0, +1 to handle device clock drift
            for (int i = -1; i <= 1; i++)
            {
                long checkStep = step + i;
                if (GenerateCode(secretBytes, checkStep) == code)
                {
                    return true;
                }
            }

            return false;
        }

        private string GenerateCode(byte[] secretBytes, long step)
        {
            var stepBytes = BitConverter.GetBytes(step);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(stepBytes);
            }

            using (var hmac = new HMACSHA1(secretBytes))
            {
                var hash = hmac.ComputeHash(stepBytes);
                int offset = hash[hash.Length - 1] & 0xf;
                int binary = ((hash[offset] & 0x7f) << 24)
                             | ((hash[offset + 1] & 0xff) << 16)
                             | ((hash[offset + 2] & 0xff) << 8)
                             | (hash[offset + 3] & 0xff);

                int otp = binary % 1000000;
                return otp.ToString("D6");
            }
        }

        private static string ToBase32String(byte[] bytes)
        {
            var sb = new StringBuilder();
            int byteCount = bytes.Length;
            for (int i = 0; i < byteCount; i += 5)
            {
                int limit = Math.Min(5, byteCount - i);
                long val = 0;
                for (int j = 0; j < limit; j++)
                {
                    val = (val << 8) | bytes[i + j];
                }
                int bits = limit * 8;
                while (bits > 0)
                {
                    bits -= 5;
                    if (bits >= 0)
                    {
                        sb.Append(Base32Alphabet[(int)((val >> bits) & 0x1f)]);
                    }
                    else
                    {
                        sb.Append(Base32Alphabet[(int)((val << -bits) & 0x1f)]);
                    }
                }
            }
            return sb.ToString();
        }

        private static byte[] FromBase32String(string base32)
        {
            base32 = base32.Trim().ToUpperInvariant().Replace("=", "");
            if (string.IsNullOrEmpty(base32))
            {
                return Array.Empty<byte>();
            }

            var list = new List<byte>();
            int bits = 0;
            int val = 0;
            foreach (char c in base32)
            {
                int idx = Base32Alphabet.IndexOf(c);
                if (idx < 0)
                {
                    throw new ArgumentException("Invalid base32 character: " + c);
                }
                val = (val << 5) | idx;
                bits += 5;
                if (bits >= 8)
                {
                    bits -= 8;
                    list.Add((byte)((val >> bits) & 0xff));
                }
            }
            return list.ToArray();
        }
    }
}
