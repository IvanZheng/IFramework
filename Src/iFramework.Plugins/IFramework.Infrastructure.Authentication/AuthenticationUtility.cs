using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Security;

namespace IFramework.Infrastructure.WebAuthentication
{
    public static class AuthenticationUtility
    {
        static AuthenticationUtility()
        {
            var rsa = new RSACryptoServiceProvider();
            RsaPrivateKey = rsa.ToXmlString(true);
            RsaPublicKey = new RsaPublicKey(rsa.ExportParameters(true));
        }

        public static RsaPublicKey RsaPublicKey { get; set; }
        private static string RsaPrivateKey { get; }

        public static T GetCurrentUser<T>() where T : class, new()
        {
            var user = default(T);
            var token = GetCurrentAuthToken<T>();
            if (token != null)
            {
                user = token.User;
            }
            return user;
        }

        public static AuthenticationToken<T> GetCurrentAuthToken<T>() where T : class, new()
        {
            var authToken = HttpContext.Current.Items[FormsAuthentication.FormsCookieName] as AuthenticationToken<T>;
            if (authToken == null)
            {
                try
                {
                    var decryptedToken = FormsAuthentication.Decrypt(
                                                                     CookiesHelper.GetCookieValue(
                                                                                                  FormsAuthentication.FormsCookieName))
                                                            .UserData;

                    if (!string.IsNullOrWhiteSpace(decryptedToken))
                    {
                        authToken = decryptedToken.ToJsonObject<AuthenticationToken<T>>();
                        HttpContext.Current.Items[FormsAuthentication.FormsCookieName] = authToken;
                    }
                }
                catch (Exception) { }
            }
            return authToken;
        }

        public static void SetAuthentication<T>(string username, T user) where T : class, new()
        {
            var token = new AuthenticationToken<T>(user);

            var ticket = new FormsAuthenticationTicket(1, // 版本号。 
                                                       username, // 与身份验证票关联的用户名。 
                                                       DateTime.Now, // Cookie 的发出时间。 
                                                       DateTime.MaxValue, // Cookie 的到期日期。 
                                                       false, // 如果 Cookie 是持久的，为 true；否则为 false。 
                                                       token.ToJson()); // 将存储在 Cookie 中的用户定义数据。  roles是一个角色字符串数组 

            var encryptedTicket = FormsAuthentication.Encrypt(ticket); //加密 
            CookiesHelper.AddCookie(FormsAuthentication.FormsCookieName, encryptedTicket,
                                    FormsAuthentication.CookieDomain);
        }

        public static void Logout()
        {
            FormsAuthentication.SignOut();
        }

        public static string BytesToHexString(byte[] input)
        {
            var hexString = new StringBuilder(64);

            for (var i = 0; i < input.Length; i++)
            {
                hexString.Append(string.Format("{0:X2}", input[i]));
            }
            return hexString.ToString();
        }

        public static byte[] HexStringToBytes(string hex)
        {
            if (hex.Length == 0)
            {
                return new byte[] {0};
            }

            if (hex.Length % 2 == 1)
            {
                hex = "0" + hex;
            }

            var result = new byte[hex.Length / 2];

            for (var i = 0; i < hex.Length / 2; i++)
            {
                result[i] = byte.Parse(hex.Substring(2 * i, 2), NumberStyles.AllowHexSpecifier);
            }

            return result;
        }

        public static string DecryptRsaPassword(string encryptedPassword)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(RsaPrivateKey);
            var result = rsa.Decrypt(HexStringToBytes(encryptedPassword), false);
            return Encoding.GetEncoding("utf-8").GetString(result);
        }
    }
}