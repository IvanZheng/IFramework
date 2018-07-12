using System;
using System.Linq;
using System.Threading.Tasks;
using IFramework.AspNet;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Sample.CommandServiceCore.Controllers
{
    [ServiceFilter(typeof(IApiResultWrapAttribute))]
    [Route("api/[controller]")]
    public class ApiControllerBase : IFramework.AspNet.ApiControllerBase
    {
        public ApiControllerBase(IConcurrencyProcessor concurrencyProcessor)
            : base(concurrencyProcessor)
        {
        }
    }
}