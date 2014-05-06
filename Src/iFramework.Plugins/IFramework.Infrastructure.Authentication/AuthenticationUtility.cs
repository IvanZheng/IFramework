using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using IFramework.Infrastructure;
using System.Web.Security;
using System.Security.Cryptography;

namespace IFramework.Infrastructure.WebAuthentication
{
    public static class AuthenticationUtility
    {
        public static RsaPublicKey RsaPublicKey { get; set; }
        static string RsaPrivateKey { get; set; }

        static AuthenticationUtility()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            RsaPrivateKey = rsa.ToXmlString(true);
            RsaPublicKey = new RsaPublicKey(rsa.ExportParameters(true));
        }

        public static T GetCurrentUser<T>() where T : class, new()
        {
            T user = default(T);
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
                    var encryptedToken = //Utility.MD5Decrypt(
                                Encoding.UTF8.GetString(
                                    Convert.FromBase64String(
                                        FormsAuthentication.Decrypt(
                                            CookiesHelper.GetCookieValue(
                                                FormsAuthentication.FormsCookieName)).UserData));//);

                    if (!string.IsNullOrWhiteSpace(encryptedToken))
                    {
                        authToken = encryptedToken.ToJsonObject<AuthenticationToken<T>>();
                        HttpContext.Current.Items[FormsAuthentication.FormsCookieName] = authToken;
                    }
                }
                catch (Exception)
                {

                }
            }
            return authToken;
        }

        public static void SetAuthentication<T>(string username, T user) where T : class, new()
        {
            var token = new AuthenticationToken<T>(user);
            //var encryptedToken = Utility.MD5Encrypt(token.ToJson());
            var encryptedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token.ToJson()));
            var ticket = new FormsAuthenticationTicket(1, // 版本号。 
                                       username, // 与身份验证票关联的用户名。 
                                       DateTime.Now, // Cookie 的发出时间。 
                                       DateTime.MaxValue,// Cookie 的到期日期。 
                                       false, // 如果 Cookie 是持久的，为 true；否则为 false。 
                                       encryptedToken); // 将存储在 Cookie 中的用户定义数据。  roles是一个角色字符串数组 

            string encryptedTicket = FormsAuthentication.Encrypt(ticket); //加密 
            CookiesHelper.AddCookie(FormsAuthentication.FormsCookieName, encryptedTicket, FormsAuthentication.CookieDomain);
        }

        public static void Logout()
        {
            FormsAuthentication.SignOut();
        }

        public static string BytesToHexString(byte[] input)
        {
            StringBuilder hexString = new StringBuilder(64);

            for (int i = 0; i < input.Length; i++)
            {
                hexString.Append(String.Format("{0:X2}", input[i]));
            }
            return hexString.ToString();
        }

        public static byte[] HexStringToBytes(string hex)
        {
            if (hex.Length == 0)
            {
                return new byte[] { 0 };
            }

            if (hex.Length % 2 == 1)
            {
                hex = "0" + hex;
            }

            byte[] result = new byte[hex.Length / 2];

            for (int i = 0; i < hex.Length / 2; i++)
            {
                result[i] = byte.Parse(hex.Substring(2 * i, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
            }

            return result;
        }

        public static string DecryptRsaPassword(string encryptedPassword)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(RsaPrivateKey);
            byte[] result = rsa.Decrypt(AuthenticationUtility.HexStringToBytes(encryptedPassword), false);
            return Encoding.GetEncoding("utf-8").GetString(result);
        }
    }
}
