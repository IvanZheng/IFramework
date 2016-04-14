using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using System.Security.Claims;

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
    }
}
