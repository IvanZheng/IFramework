using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Config
{
    public static class IFrameworkConfigurationExtension
    {
        static Type _CustomSecurityTokenService;

        public static void SetCustomSecurityTokenServiceType(this Configuration configuration, Type customSecurityTokenServiceType)
        {
            _CustomSecurityTokenService = customSecurityTokenServiceType;
        }
        public static Type GetCustomSecurityTokenServiceType(this Configuration configuration)
        {
            if (_CustomSecurityTokenService == null)
            {
                throw new NotSupportedException("should call SetCustomSecurityTokenService first!");
            }
            return _CustomSecurityTokenService;
        }
    }
}
