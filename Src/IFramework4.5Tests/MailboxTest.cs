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

namespace IFramework.Infrastructure.Tests
{
    [TestClass()]
    public class MailboxTest
    {
        int _totalProcessed = 0;
        [TestMethod()]
        public void ScheduleMailboxTest()
        {
            var processor = new MessageProcessor(new DefaultProcessingMessageScheduler<IMessageContext>());

            int i = 100;
            int j = 10;
            int totalShouldbe = i * j;
            while (--i >= 0)
            {
                int k = j;
                while (--k >= 0)
                {
                    var messageContext = new MessageContext
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

            //Assert.AreEqual(i * j, _totalProcessed);
        }

        void ProcessingMessage(IMessageContext messageContext)
        {
            Trace.WriteLine(string.Format("order: {1} process: {0}", messageContext.MessageID, _totalProcessed));
            Interlocked.Add(ref _totalProcessed, 1);
        }
    }
}