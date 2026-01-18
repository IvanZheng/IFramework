using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Sample.DTO;

namespace Sample.CommandServiceCore.Controllers
{
    public class UserController : ApiControllerBase
    {
        public UserController(IConcurrencyProcessor concurrencyProcessor) : base(concurrencyProcessor)
        {
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpGet]
        public Account Index(CommonStatus? status)
        {
            return new Account();
        }
    }
}
