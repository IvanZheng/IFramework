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
        public ValuesController(IExceptionManager exceptionManager) : base(exceptionManager) { }

        public async Task<ApiResult<string[]>> Get()
        {
            return await ProcessAsync(() => Task.FromResult(new[] {"value1", "value2"}));
        }

        // GET api/<controller>/5
       
        public string Get(int id)
        {
            return "value";
        }

        public ApiResult<string[]> Post([FromBody] string value)
        {
            return Process(() =>
            {
                Task.Delay(1000).Wait();
                return new[] { "value1", "value2" };
            });
        }


        // POST api/<controller>
        //public Task<ApiResult<string[]>> Post([FromBody] string value)
        //{
        //    return ProcessAsync(async () =>
        //    {
        //        await Task.Delay(1000);
        //        return new[] {"value1", "value2"};
        //    });
        //}

        // PUT api/<controller>/5
        public void Put(int id, [FromBody] string value) { }

        // DELETE api/<controller>/5
        public void Delete(int id) { }
    }
}