using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Infrastructure.Logging;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.IoC;
using IFramework.Message;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IFramework4._5Tests
{
    [TestClass]
    public class MailboxTest
    {
        private readonly ILogger _logger;
        private int _totalProcessed;

        public MailboxTest()
        {
            Configuration.Instance.UseLog4Net();
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(MailboxTest));
        }

        [TestMethod]
        public void ScheduleMailboxTest()
        {
            var processor = new MessageProcessor(new DefaultProcessingMessageScheduler<IMessageContext>());
            processor.Start();
            var i = 1000;
            var j = 1000;
            var totalShouldbe = i * j;
            while (--i >= 0)
            {
                var k = j;
                while (--k >= 0)
                {
                    IMessageContext messageContext = new MessageContext
                    {
                        MessageID = string.Format("batch:{0}-key:{1}", i, k),
                        Key = k.ToString()
                    };
                    processor.Process(messageContext, ProcessingMessage);
                }
            }

            while (_totalProcessed != totalShouldbe)
            {
                Task.Delay(1000).Wait();
            }

            Assert.AreEqual(processor.MailboxDictionary.Count, 0);
        }

        private async Task ProcessingMessage(IMessageContext messageContext)
        {
            _logger.DebugFormat("order: {1} process: {0}", messageContext.MessageID, _totalProcessed);
            Interlocked.Add(ref _totalProcessed, 1);
            await Task.FromResult(true);
        }
    }
}