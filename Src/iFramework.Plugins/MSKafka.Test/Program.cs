using IFramework.MessageQueue.MSKafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.MessageQueue.MSKafka.MessageFormat;
using IFramework.Config;
using Sample.Command;
using IFramework.Command;
using IFramework.IoC;
using Sample.DTO;
using IFramework.Infrastructure;

namespace MSKafka.Test
{
    class Program
    {
        static string commandQueue = "commandqueueA";
        static string replyTopic = "replyTopic";
        static string eventTopic = "eventTopic";
        static string subscription = "testSubscription";
        static int total = 0;
        static void Main(string[] args)
        {
            Configuration.Instance.UseUnityContainer()
                                  .MessageQueueUseMachineNameFormat(false)
                                  .UseLog4Net("log4net.config");

            ServiceTest();
            return;

            string zkConnectionString = "192.168.99.60:2181";
            var client = new KafkaClient(zkConnectionString);

            //var queueClient = client.GetQueueClient(Configuration.Instance.FormatMessageQueueName(commandQueue));
            //queueClient.CommitOffset(4);

            client.StartQueueClient(commandQueue, "0", messageContexts =>
            {
                messageContexts.ForEach(messageContext =>
                {
                    var kafakMessageContext = messageContext as MessageContext;
                    var command = messageContext.Message as Command;

                    var val = 0;
                    int.TryParse(command.Body, out val);
                    total += val;
                    Console.WriteLine($"handle command {command.ID} message: {command.Body} offset:{kafakMessageContext.Offset}");
                    //kafakMessageContext.CommitOffset();
                    //  publish reply
                    var reply = $"cmd {command.Body} reply";
                    var messageReply = client.WrapMessage(reply, messageContext.MessageID, messageContext.ReplyToEndPoint);
                    client.Publish(messageReply, replyTopic);

                    // publish event
                    var @event = new DomainEvent($"handled event {command.Body}");
                    client.Publish(new MessageContext(@event), eventTopic);
                });

            });

            client.StartSubscriptionClient(replyTopic, subscription, "0", messageContexts =>
            {
                messageContexts.ForEach(messageContext => {
                    var kafakMessageContext = messageContext as MessageContext;
                    var reply = messageContext.Message;
                    Console.WriteLine($"reply receive {reply} offset:{kafakMessageContext.Offset}");
                });
            });

            client.StartSubscriptionClient(eventTopic, subscription, "0", messageContexts =>
            {
                messageContexts.ForEach(messageContext => {
                    var kafakMessageContext = messageContext as MessageContext;
                    var @event = messageContext.Message as DomainEvent;
                    Console.WriteLine($"subscription receive {@event.Body} offset:{kafakMessageContext.Offset}");
                });
            });


            //for(int i = 0; i < 5; i ++)
            //{
            //    SendCommand(client, i.ToString());
            //}

            while (true)
            {
                try
                {
                    var body = Console.ReadLine();
                    if (!string.IsNullOrEmpty(body))
                    {
                        SendCommand(client, body);
                    }
                    else
                    {
                        Console.WriteLine($"total is {total}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetBaseException().Message);
                }
            }
        }


        static void SendCommand(KafkaClient client, string body)
        {
            var command = new Command(body);
            client.Send(new MessageContext(command), commandQueue);
            Console.WriteLine($"Send {command.ID} successfully cmd: {command.Body}");
        }


        static void ServiceTest()
        {
            ReduceProduct reduceProduct = new ReduceProduct
            {
                ProductId = new Guid("2B6FDE83-A319-433B-9FA5-399D382D0CD3"),
                ReduceCount = 1
            };

            var _commandBus = IoCFactory.Resolve<ICommandBus>();
            _commandBus.Start();

            var t = _commandBus.SendAsync(reduceProduct, true).Result;
            Console.WriteLine(t.Reply.Result);


            var products = _commandBus.SendAsync(new GetProducts
            {
                ProductIds = new List<Guid> { reduceProduct.ProductId }
            }, true).Result.ReadAsAsync<List<Project>>().Result;

            Console.WriteLine(products.ToJson());
            Console.ReadLine();
        }
    }
}
