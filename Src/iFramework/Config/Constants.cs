using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Config
{
    public static class Constants
    {
        public static class Configuration
        {
            public const string DomainEventProvider = @"DomainEventProvider";
            public const string CommandProvider = @"CommandProvider";

            public const string DomainEventBus = @"DomainEventBus";
            public const string CommandService = @"CommandService";

            public const string DomainModelContext = @"DomainModelContext";
            public const string ReadModelContext = @"ReadModelContext";
        }
    }
}
