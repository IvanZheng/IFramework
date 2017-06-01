using System;

namespace IFramework.Config
{
    public static class IFrameworkConfigurationExtension
    {
        private static Type _CustomSecurityTokenService;

        public static void SetCustomSecurityTokenServiceType(this Configuration configuration,
                                                             Type customSecurityTokenServiceType)
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