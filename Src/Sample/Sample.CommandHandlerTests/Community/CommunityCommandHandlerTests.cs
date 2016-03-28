using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sample.CommandHandler.Community;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Command;
using IFramework.Message;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.Infrastructure;
using IFramework.Config;
using IFramework.Command;
using Sample.CommandHandlerTests;

namespace Sample.CommandHandler.Community.Tests
{
    [TestClass()]
    public class CommunityCommandHandlerTests : CommandHandlerTest<CommunityCommandHandler>
    {
        static string _UserName;
        public CommunityCommandHandlerTests()
        {
            Configuration.Instance.UseLog4Net();

        }

        [TestMethod()]
        public void LoginHandleTest()
        {
            Login registerCommand = new Login
            {
                UserName = "ivan",
                Password = "123456"
            };
            var result = ExecuteCommand(registerCommand);
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public void RegisterHandleTest()
        {
            Register registerCommand = new Register { 
                 UserName = "ivan" + DateTime.Now.ToString("HH:mm:ss"),
                 Password = "1234"
            };
            var result = ExecuteCommand(registerCommand);
            _UserName = registerCommand.UserName;
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public void ModifyHandleTest()
        {
            Modify modifyCommand = new Modify { 
                 Email = "haojie77@163.com",
                 UserName = _UserName
            };
            ExecuteCommand(modifyCommand);
        }
    }
}
