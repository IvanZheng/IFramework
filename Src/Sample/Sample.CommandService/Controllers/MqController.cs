using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using IFramework.AspNet;
using IFramework.Infrastructure;

namespace Sample.CommandService.Controllers
{
    public class MqController: ApiControllerBase
    {
        [HttpGet, Route("api/mq/CloseMessageQueue")]
        public ApiResult CloseMessageQueue()
        {
            return Process(WebApiApplication.CloseMessageQueue);
        }
       
    }
}