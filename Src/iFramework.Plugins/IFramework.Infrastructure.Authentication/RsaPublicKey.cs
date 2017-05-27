using System.Security.Cryptography;

namespace IFramework.Infrastructure.WebAuthentication
{
    public class RsaPublicKey
    {
        public RsaPublicKey(RSAParameters parameters)
        {
            Exponent = AuthenticationUtility.BytesToHexString(parameters.Exponent);
            Modulus = AuthenticationUtility.BytesToHexString(parameters.Modulus);
        }

        public string Exponent { get; set; }
        public string Modulus { get; set; }
    }
}