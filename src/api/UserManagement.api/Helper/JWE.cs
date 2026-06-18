namespace UserManagement.Helper
{
    public class JWE
    {
        //public static void GenerateKeys()
        //{
        //    // Static keys for encryption and signing
        //    string encryptionKeyString = "AhUIXvrU2xj0zx49yK1CpsfJOZRo3GE8";
        //    string signingKeyString = "vl6e2xsPdxzCwa0YKCN6LLMAecHcykZJ";

        //    // Convert the static encryption and signing keys to appropriate types
        //    var encryptionKeyBytes = Convert.FromBase64String(encryptionKeyString);
        //    var signingKeyBytes = Convert.FromBase64String(signingKeyString);

        //    var encryptionParameters = new RSAParameters
        //    {
        //        Modulus = encryptionKeyBytes,
        //        Exponent = new byte[] { 1, 0, 1 }
        //    };

        //    var encryptionKey = new RsaSecurityKey(encryptionParameters) { KeyId = Guid.NewGuid().ToString("N") };

        //    var signingKey = ECDsa.Create();
        //    signingKey.ImportECPrivateKey(signingKeyBytes, out _);
        //    var signingParameters = signingKey.ExportParameters(false);

        //    var encryptionKid = Guid.NewGuid().ToString("N");
        //    var signingKid = Guid.NewGuid().ToString("N");

        //    var privateEncryptionKey = new RsaSecurityKey(encryptionParameters) { KeyId = encryptionKid };
        //    var publicEncryptionKey = new RsaSecurityKey(encryptionParameters) { KeyId = encryptionKid };

        //    var privateSigningKey = new ECDsaSecurityKey(signingParameters) { KeyId = signingKid };
        //    var publicSigningKey = new ECDsaSecurityKey(signingParameters) { KeyId = signingKid };

        //    // Rest of your code here
        //}
    }
}
