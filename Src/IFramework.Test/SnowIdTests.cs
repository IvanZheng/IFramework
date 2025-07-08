using IFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IFramework.Test
{
    public class SnowIdTests
    {
        
        [Fact]
        public void IdTest()
        {
            var id = SnowIdWorker.GenerateLongId();

            var dateTime = SnowIdWorker.ExtractTimestamp(id);

            Assert.Equal(dateTime.Date, DateTime.Now.Date);
        }
    }
}
