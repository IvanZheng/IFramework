using System;
using System.Security.Cryptography;
using System.Text;

namespace IFramework.Configuration.ProtectedJson
{
    public static class AesEncryptor
    {
        public static string Encrypt(string plainText, string key)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);

            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform cTransform = aes.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            return Convert.ToBase64String(resultArray);
        }

        public static string Decrypt(string cipherText, string key)
        {
            var cipherBytes = Convert.FromBase64String(cipherText);

            SymmetricAlgorithm aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform cTransform = aes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(resultArray);
        }
    }
}