using System;
using IFramework.Config;
using IFramework.EntityFramework.Config;
using IFramework.Infrastructure;
using IFramework.IoC;
using IFramework.MessageQueue;
using IFramework.MessageQueue.ConfluentKafka.Config;
using IFramework.MessageQueue.MSKafka.Config;
using Sample.Domain;
using Sample.Persistence;
using Sample.Persistence.Repositories;

namespace Sample.CommandConsumer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Configuration.Instance
                             .UseUnityContainer()
                             .RegisterCommonComponents()
                             .UseLog4Net()
                             .MessageQueueUseMachineNameFormat()
                             .UseMessageQueue()
                             .UseMessageStore<SampleModelContext>()
                             .UseConfluentKafka("192.168.99.60:2181")
                             .UseMessagePublisher("eventTopic")
                             .RegisterEntityFrameworkComponents();

                var container = IoCFactory.Instance.CurrentContainer;
                container.RegisterType<ICommunityRepository, CommunityRepository>(Lifetime.Hierarchical);
                container.RegisterType<SampleModelContext, SampleModelContext>(Lifetime.Hierarchical);

                #region EventPublisher init

                var messagePublisher = MessageQueueFactory.GetMessagePublisher();
                messagePublisher.Start();

                #endregion

                #region CommandConsumer init

                var commandQueueName = "commandqueue";
                var commandConsumer = MessageQueueFactory.CreateCommandConsumer(commandQueueName,
                                                                                ObjectId.GenerateNewId().ToString(),
                                                                                new []{"CommandHandlers"},
                                                                                ConsumerConfig.DefaultConfig);
                commandConsumer.Start();

                #endregion

                Console.ReadLine();

                #region stop service

                commandConsumer.Stop();
                messagePublisher.Stop();

                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetBaseException().Message);
            }
        }
    }
}