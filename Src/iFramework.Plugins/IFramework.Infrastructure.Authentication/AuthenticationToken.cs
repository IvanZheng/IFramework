using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace IFramework.Infrastructure.WebAuthentication
{
    public class AuthenticationToken<T>
        where T : class, new()
    {
        public T User { get; set; }
        public string ClientIP { get; set; }


        public AuthenticationToken(T user)
        {
            User = user;
            ClientIP = HttpContext.Current.Request.UserHostAddress;
        }
    }
}
