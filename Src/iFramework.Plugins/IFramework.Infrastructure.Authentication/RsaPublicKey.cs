using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace IFramework.Infrastructure.WebAuthentication
{
    public class RsaPublicKey
    {
        public string Exponent { get; set; }
        public string Modulus { get; set; }

        public RsaPublicKey(RSAParameters parameters)
        {
            Exponent = AuthenticationUtility.BytesToHexString(parameters.Exponent);
            Modulus = AuthenticationUtility.BytesToHexString(parameters.Modulus);
        }
    }
}
