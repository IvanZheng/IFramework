using Microsoft.VisualStudio.TestTools.UnitTesting;
using IFramework.Infrastructure.Mailboxes.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework4._5Tests;
using IFramework.Message;
using System.Threading;
using System.Diagnostics;
using IFramework.Config;
using IFramework.Infrastructure.Logging;
using IFramework.Infrastructure;
using IFramework.IoC;

namespace IFramework4._5Tests
{
    [TestClass()]
    public class MailboxTest
    {
        int _totalProcessed = 0;
        ILogger _logger;
        public MailboxTest()
        {
            Configuration.Instance.UseLog4Net();
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(MailboxTest));
        }

        [TestMethod()]
        public void ScheduleMailboxTest()
        {
            var processor = new MessageProcessor(new DefaultProcessingMessageScheduler<IMessageContext>());
            processor.Start();
            int i = 1000;
            int j = 1000;
            int totalShouldbe = i * j;
            while (--i >= 0)
            {
                int k = j;
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

        void ProcessingMessage(IMessageContext messageContext)
        {
            _logger.DebugFormat("order: {1} process: {0}", messageContext.MessageID, _totalProcessed);
            Interlocked.Add(ref _totalProcessed, 1);
        }
    }
}