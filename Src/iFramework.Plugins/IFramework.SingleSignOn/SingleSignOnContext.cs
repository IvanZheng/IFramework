using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using System.Security.Claims;
using System.IdentityModel.Services;
using System.Web.Security;

namespace IFramework.SingleSignOn
{
    public class SingleSignOnContext<TUser> where TUser : class
    {
        public static TUser CurrentUser
        {
            get
            {
                try
                {
                    var user = HttpContext.Current.Items["ssouser"] as TUser;
                    if (user == null && HttpContext.Current.User != null)
                    {
                        var claimsIdentity = HttpContext.Current.User.Identity as ClaimsIdentity;
                        if (claimsIdentity != null)
                        {
                            var claim = claimsIdentity.FindFirst("Iframework/Info");
                            if (claim != null)
                            {
                                var userInfo = claim.Value;
                                user = JsonConvert.DeserializeObject<TUser>(userInfo);
                                HttpContext.Current.Items["ssouser"] = user;
                            }
                        }
                    }
                    return user;
                }
                catch (Exception)
                {
                    return null;
                }
              
            }
        }

        public static SignOutRequestMessage SignOut(string signOutUrl)
        {
            var fam = FederatedAuthentication.WSFederationAuthenticationModule;
            try
            {
                FormsAuthentication.SignOut();
            }
            finally
            {
                fam.SignOut(true);
            }
            var currentAudienceUri = GetCurrentAudienceUri();
            return new SignOutRequestMessage(new Uri(new Uri(fam.Issuer), signOutUrl), currentAudienceUri.AbsoluteUri);
        }

        /// <summary>
        /// 获取当前AudienceUri
        /// </summary>
        /// <returns>当前AudienceUri</returns>
        public static Uri GetCurrentAudienceUri()
        {
            var audienceUris = FederatedAuthentication.WSFederationAuthenticationModule.FederationConfiguration.IdentityConfiguration.AudienceRestriction.AllowedAudienceUris;
            var currentUrl = HttpContext.Current.Request.Url.AbsoluteUri.ToLower();
            var compareUrl = currentUrl.EndsWith("/") ? currentUrl : currentUrl + "/";
            var currentAudienceUri = audienceUris.FirstOrDefault(ent => compareUrl.ToLower().Contains(ent.AbsoluteUri.ToLower()));
            if (currentAudienceUri == null)
            {
                throw new Exception("未找到到与访问地址匹配的audienceUri,请检查配置文件中的audienceUris");
            }
            return currentAudienceUri;
        }
    }
}
