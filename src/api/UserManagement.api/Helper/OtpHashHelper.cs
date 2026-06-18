using System.Security.Cryptography;
using System.Text;
namespace UserManagement.Helper
{
    public static class OtpHashHelper
    {
        public static (string hash, string salt) GenerateOtpHash(
            string otp,
            string privateKey)
        {
            // Generate random salt
            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            string salt = Convert.ToBase64String(saltBytes);

            // Combine OTP + Salt
            var otpWithSalt = otp + salt;

            // Create HMACSHA256 hash using private key
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(privateKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(otpWithSalt));

            string hash = Convert.ToBase64String(hashBytes);

            return (hash, salt);
        }
    }
}
