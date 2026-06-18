using System;
using System.Security.Cryptography;
using System.Text;
using QRCoder;

namespace backend.Helpers
{
    /// <summary>
    /// TOTP (Time-based One-Time Password) Helper
    /// RFC 6238 compliant implementation
    /// Works with Google Authenticator, Microsoft Authenticator, Authy, etc.
    /// </summary>
    public class TOTPHelper
    {
        // Standard TOTP time step in seconds (30 seconds)
        private const int TimeStep = 30;
        
        // Default TOTP digit length (6 digits)
        private const int DefaultCodeLength = 6;

        /// <summary>
        /// Generate a random base32-encoded TOTP secret
        /// Used for initial TOTP setup
        /// </summary>
        /// <returns>Base32-encoded TOTP secret</returns>
        public static string GenerateSecret()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] secretBuffer = new byte[20]; // 160 bits for 32 characters in base32
                rng.GetBytes(secretBuffer);
                return Base32Encode(secretBuffer);
            }
        }

        /// <summary>
        /// Verify a TOTP code against a secret
        /// Allows ±3 time windows for clock skew tolerance
        /// </summary>
        /// <param name="secret">Base32-encoded TOTP secret</param>
        /// <param name="code">6-digit code to verify</param>
        /// <param name="toleranceWindows">Number of 30-second windows to tolerate on each side (default: 3, allows ±90 seconds)</param>
        /// <returns>True if code is valid, false otherwise</returns>
        public static bool VerifyCode(string secret, string code, int toleranceWindows = 3)
        {
            try
            {
                if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code))
                {
                    System.Diagnostics.Debug.WriteLine($"TOTP Verification: Empty secret or code. Secret={!string.IsNullOrEmpty(secret)}, Code={!string.IsNullOrEmpty(code)}");
                    return false;
                }

                if (!int.TryParse(code, out int codeInt))
                {
                    System.Diagnostics.Debug.WriteLine($"TOTP Verification: Code is not numeric: {code}");
                    return false;
                }

                if (code.Length != DefaultCodeLength)
                {
                    System.Diagnostics.Debug.WriteLine($"TOTP Verification: Code length mismatch. Expected 6, got {code.Length}: {code}");
                    return false;
                }

                byte[] secretBytes = Base32Decode(secret);
                System.Diagnostics.Debug.WriteLine($"TOTP Verification: Secret decoded to {secretBytes.Length} bytes");
                
                if (secretBytes.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"TOTP Verification: Base32 decode produced empty bytes");
                    return false;
                }

                // Get the current time counter
                long currentCounter = GetTimeCounter();
                System.Diagnostics.Debug.WriteLine($"TOTP Verification: Current counter={currentCounter}, Tolerance windows={toleranceWindows}");

                // Check current time window and adjacent windows for tolerance (each window is 30 seconds)
                for (int i = -toleranceWindows; i <= toleranceWindows; i++)
                {
                    long testCounter = currentCounter + i;
                    string expectedCode = GenerateCode(secretBytes, testCounter);
                    System.Diagnostics.Debug.WriteLine($"TOTP Verification: Window offset {i}: expecting {expectedCode}, got {code}");
                    
                    if (expectedCode == code)
                    {
                        System.Diagnostics.Debug.WriteLine($"TOTP Verification: SUCCESS at window offset {i}");
                        return true;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"TOTP Verification: FAILED - no matching code found");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TOTP Verification Error: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Generate current TOTP code for a secret
        /// Useful for testing/debugging
        /// </summary>
        /// <param name="secret">Base32-encoded TOTP secret</param>
        /// <returns>Current 6-digit TOTP code</returns>
        public static string GetCurrentCode(string secret)
        {
            try
            {
                if (string.IsNullOrEmpty(secret))
                    return "";

                byte[] secretBytes = Base32Decode(secret);
                long currentCounter = GetTimeCounter();
                return GenerateCode(secretBytes, currentCounter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TOTP Generation Error: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Generate TOTP code for a specific time counter value
        /// </summary>
        private static string GenerateCode(byte[] secret, long counter)
        {
            byte[] counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(counterBytes);

            using (var hmac = new HMACSHA1(secret))
            {
                byte[] hash = hmac.ComputeHash(counterBytes);
                
                // Dynamic truncation (RFC 4226)
                int offset = hash[hash.Length - 1] & 0x0f;
                int code = (hash[offset] & 0x7f) << 24
                    | (hash[offset + 1] & 0xff) << 16
                    | (hash[offset + 2] & 0xff) << 8
                    | (hash[offset + 3] & 0xff);

                code = code % (int)Math.Pow(10, DefaultCodeLength);
                return code.ToString().PadLeft(DefaultCodeLength, '0');
            }
        }

        /// <summary>
        /// Get current UNIX timestamp divided by time step
        /// </summary>
        private static long GetTimeCounter()
        {
            // Use DateTimeOffset.UtcNow to get Unix timestamp directly
            // This avoids any timezone/DST issues
            long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long counter = unixTimestamp / TimeStep;
            System.Diagnostics.Debug.WriteLine($"TOTP TimeCounter: Unix timestamp={unixTimestamp}, Counter={counter}");
            return counter;
        }

        /// <summary>
        /// Encode bytes to Base32 string (RFC 4648)
        /// </summary>
        private static string Base32Encode(byte[] input)
        {
            if (input == null || input.Length == 0)
                return string.Empty;

            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var sb = new StringBuilder();
            
            int bits = 0;
            int value = 0;

            foreach (byte b in input)
            {
                value = (value << 8) | b;
                bits += 8;
                
                while (bits >= 5)
                {
                    bits -= 5;
                    sb.Append(alphabet[(value >> bits) & 31]);
                }
            }

            if (bits > 0)
            {
                sb.Append(alphabet[(value << (5 - bits)) & 31]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Decode Base32 string to bytes (RFC 4648)
        /// </summary>
        private static byte[] Base32Decode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return Array.Empty<byte>();

            input = input.ToUpperInvariant();
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            
            var ms = new MemoryStream();
            
            int bits = 0;
            int value = 0;

            foreach (char c in input)
            {
                if (c == '=') break;
                
                int idx = alphabet.IndexOf(c);
                if (idx < 0)
                    throw new ArgumentException($"Invalid Base32 character: {c}");

                value = (value << 5) | idx;
                bits += 5;

                if (bits >= 8)
                {
                    bits -= 8;
                    ms.WriteByte((byte)((value >> bits) & 255));
                }
            }

            return ms.ToArray();
        }

        /// <summary>
        /// Generate QR code provisioning URI for setting up TOTP in authenticator app
        /// </summary>
        /// <param name="secret">Base32-encoded TOTP secret</param>
        /// <param name="accountName">Account name (usually username or email)</param>
        /// <param name="issuerName">Issuer name (your app/organization name)</param>
        /// <returns>otpauth:// URI for QR code generation</returns>
        public static string GetQRCodeProvisioningUri(string secret, string accountName, string issuerName = "UserManagement")
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(accountName))
                return "";

            string encodedAccountName = Uri.EscapeDataString(accountName);
            string encodedIssuerName = Uri.EscapeDataString(issuerName);
            
            return $"otpauth://totp/{encodedIssuerName}:{encodedAccountName}?secret={secret}&issuer={encodedIssuerName}&period={TimeStep}&digits={DefaultCodeLength}&algorithm=SHA1";
        }

        /// <summary>
        /// Generate QR code image as PNG bytes for the given provisioning URI
        /// Uses QRCoder NuGet package
        /// </summary>
        /// <param name="provisioningUri">otpauth:// URI from GetQRCodeProvisioningUri</param>
        /// <returns>PNG image as byte array</returns>
        public static byte[] GenerateQRCodeImage(string provisioningUri)
        {
            try
            {
                if (string.IsNullOrEmpty(provisioningUri))
                    return Array.Empty<byte>();

                using (var qrGenerator = new QRCodeGenerator())
                {
                    // Create QR code with Quartile error correction level
                    var qrCodeData = qrGenerator.CreateQrCode(provisioningUri, QRCodeGenerator.ECCLevel.Q);
                    
                    // Generate PNG image (20 pixels per module)
                    using (var pngCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeImage = pngCode.GetGraphic(20);
                        return qrCodeImage ?? Array.Empty<byte>();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"QR Code Generation Error: {ex.Message}");
                return Array.Empty<byte>();
            }
        }
    }
}
