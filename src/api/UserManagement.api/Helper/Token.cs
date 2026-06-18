//using FluentAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
//using Xunit;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace UserManagement.Helper
{
    public class Token
    {
        public static string GenerateToken(List<Claim> claims, string tokenKey)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public static void GenerateKeys(List<Claim> claims)
        {
            var encryptionKey = RSA.Create(3072); // public key for encryption, private key for decryption
            var signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256); // private key for signing, public key for validation

            string encryptionKeyBase64 = ConvertToBase64(encryptionKey);
            string signingKeyBase64 = ConvertToBase64(signingKey);

            //var encryptionKid = Guid.NewGuid().ToString("N");
            //var signingKid = Guid.NewGuid().ToString("N");

            var encryptionKid = "8524e3e6674e494f85c5c775dcd602c5";
            var signingKid = "29b4adf8bcc941dc8ce40a6d0227b6d3";

            var privateEncryptionKey = new RsaSecurityKey(encryptionKey) { KeyId = encryptionKid };
            var publicEncryptionKey = new RsaSecurityKey(encryptionKey.ExportParameters(false)) { KeyId = encryptionKid };
            var privateSigningKey = new ECDsaSecurityKey(signingKey) { KeyId = signingKid };
            var publicSigningKey = new ECDsaSecurityKey(ECDsa.Create(signingKey.ExportParameters(false))) { KeyId = signingKid };

            var token = CreateJwe(privateSigningKey, publicEncryptionKey);
            bool a = DecryptAndValidateJwe(token, publicSigningKey, privateEncryptionKey);
        }

        private static string CreateJwe(SecurityKey signingKey, SecurityKey encryptionKey)
        {
            var handler = new JsonWebTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Audience = "api1",
                Issuer = "https://idp.example.com",
                Claims = new Dictionary<string, object> { { "sub", "811e790749a24d8a8f766e1a44dca28a" } },

                // private key for signing
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.EcdsaSha256),

                // public key for encryption
                EncryptingCredentials = new EncryptingCredentials(encryptionKey, SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256CbcHmacSha512)
            };

            return handler.CreateToken(tokenDescriptor);
        }

        public static void DecryptAndValidateJwe(string token, string encryptionKeyBase64, string signingKeyBase64)
        {
            var encryptionKey = ConvertFromBase64<RSA>(encryptionKeyBase64);
            var signingKey = ConvertFromBase64<ECDsa>(signingKeyBase64);
            var encryptionKid = "8524e3e6674e494f85c5c775dcd602c5";
            var signingKid = "29b4adf8bcc941dc8ce40a6d0227b6d3";
            var privateEncryptionKey = new RsaSecurityKey(encryptionKey) { KeyId = encryptionKid };
            var publicEncryptionKey = new RsaSecurityKey(encryptionKey.ExportParameters(false)) { KeyId = encryptionKid };
            var privateSigningKey = new ECDsaSecurityKey(signingKey) { KeyId = signingKid };
            var publicSigningKey = new ECDsaSecurityKey(ECDsa.Create(signingKey.ExportParameters(false))) { KeyId = signingKid };
            bool a = DecryptAndValidateJwe(token, publicSigningKey, privateEncryptionKey);
        }

        private static bool DecryptAndValidateJwe(string token, SecurityKey signingKey, SecurityKey encryptionKey)
        {
            var handler = new JsonWebTokenHandler();

            TokenValidationResult result = handler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidAudience = "api1",
                    ValidIssuer = "https://idp.example.com",

                    // public key for signing
                    IssuerSigningKey = signingKey,

                    // private key for encryption
                    TokenDecryptionKey = encryptionKey
                });

            return result.IsValid;
        }
        //public static string GenerateJWEToken(List<Claim> claims)
        //{
        //    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my-secret-key"));
        //    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        //    JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

        //    var tokenDescriptor = new SecurityTokenDescriptor
        //    {
        //        Issuer = "your-issuer",
        //        Audience = "your-audience",
        //        Subject = new ClaimsIdentity(claims),
        //        Expires = DateTime.UtcNow.AddMinutes(15),
        //        SigningCredentials = credentials,
        //        EncryptingCredentials = new EncryptingCredentials(
        //                        new X509SecurityKey(new X509Certificate2("my-certificate.pfx", "password")),
        //                        SecurityAlgorithms.RsaOAEP,
        //                        SecurityAlgorithms.Aes256CbcHmacSha512
        //                    )
        //    };

        //    // Create the JWE token.
        //    string token = handler.CreateEncodedJwt(tokenDescriptor);
        //    return token;
        //}
        //private static string ConvertKeyToBase64(SecurityKey key)
        //{
        //    if (key is RsaSecurityKey rsaKey)
        //    {
        //        var keyJson = JsonSerializer.Serialize(rsaKey.Parameters);
        //        byte[] keyBytes = Encoding.UTF8.GetBytes(keyJson);
        //        return Convert.ToBase64String(keyBytes);
        //    }
        //    else if (key is ECDsaSecurityKey ecKey)
        //    {
        //        var ecParameters = ecKey.GetECDsaPublicKey().ExportParameters(false);
        //        var keyJson = JsonSerializer.Serialize(ecParameters);
        //        byte[] keyBytes = Encoding.UTF8.GetBytes(keyJson);
        //        return Convert.ToBase64String(keyBytes);
        //    }
        //    else
        //    {
        //        throw new NotSupportedException("Unsupported key type");
        //    }
        //}
        static string ConvertToBase64(AsymmetricAlgorithm key)
        {
            // Export the key to a byte array
            byte[] keyBytes = key.ExportSubjectPublicKeyInfo();

            // Convert the byte array to Base64
            string keyBase64 = Convert.ToBase64String(keyBytes);

            return keyBase64;
        }

        static T ConvertFromBase64<T>(string keyBase64) where T : AsymmetricAlgorithm
        {
            // Convert the Base64 string to a byte array
            byte[] keyBytes = Convert.FromBase64String(keyBase64);

            // Create a new instance of the key type
            T key = Activator.CreateInstance<T>();

            // Import the key from the byte array
            key.ImportSubjectPublicKeyInfo(keyBytes, out _);

            return key;
        }
    }

}
