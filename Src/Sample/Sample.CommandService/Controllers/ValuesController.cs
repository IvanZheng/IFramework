using IFramework.AspNet;
using IFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Sample.CommandService.Controllers
{
    [IPFilter]
    public class ValuesController : ApiControllerBase
    {
        // GET api/<controller>
        public Task<ApiResult<string[]>> Get()
        {
            return ProcessAsync(() => {
                throw new Exception("test exception");
                return Task.FromResult(new string[] { "value1", "value2" });
            });
            
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}