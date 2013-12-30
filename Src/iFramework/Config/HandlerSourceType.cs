using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Config
{
    public enum HandlerSourceType
    {
        /// <summary>
        /// Indicates that the configuration value represented by the Source
        /// attribute is a type name.
        /// </summary>
        Type,
        /// <summary>
        /// Indicates that the configuration value represented by the Source
        /// attribute is an assembly name.
        /// </summary>
        Assembly
    }
}
