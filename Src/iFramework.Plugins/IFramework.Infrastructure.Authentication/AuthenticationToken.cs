using System.Web;

namespace IFramework.Infrastructure.WebAuthentication
{
    public class AuthenticationToken<T>
        where T : class, new()
    {
        public AuthenticationToken(T user)
        {
            User = user;
            ClientIP = HttpContext.Current.Request.UserHostAddress;
        }

        public T User { get; set; }
        public string ClientIP { get; set; }
    }
}