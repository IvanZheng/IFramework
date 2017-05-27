using System;
using System.Threading.Tasks;
using System.Web.Http;
using IFramework.AspNet;
using IFramework.Infrastructure;

namespace Sample.CommandService.Controllers
{
    [IPFilter]
    public class ValuesController : ApiControllerBase
    {
        // GET api/<controller>
        public Task<ApiResult<string[]>> Get()
        {
            return ProcessAsync(() =>
            {
                throw new Exception("test exception");
                return Task.FromResult(new[] {"value1", "value2"});
            });
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}