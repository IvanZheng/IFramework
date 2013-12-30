using IFramework.Message;
using IFramework.Message.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroMQ;
using IFramework.Infrastructure;
using System.Collections;
using IFramework.Command;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.ZeroMQ
{
    // 记录了command 在哪个consumer中处理, 当command是linear的时候LinearCommandConsumer不为空
    // CommandConsumer表示command在哪个consumer上处理.
    public class CommandState
    {
        public string CommandID { get; set; }
        public CommandQueueConsumer CommandConsumer { get; set; }
        public LinearCommandConsumer LinearCommandConsumer { get; set; }
    }

    // 处理linearcommand 的consuemr
    public class LinearCommandConsumer
    {
        public CommandQueueConsumer CommandConsumer { get; set; }
        public int Payload { get; set; }
        public object LinearKey { get; set; }
        public double FinishedCount { get; set; }

        public LinearCommandConsumer(CommandQueueConsumer commandConsumer, object linearKey)
        {
            CommandConsumer = commandConsumer;
            LinearKey = linearKey;
        }

        internal void PushCommandContext(IMessageContext commandContext)
        {
            CommandConsumer.PushMessageContext(commandContext);
            Payload++;
        }

        internal void CommandHandled()
        {
            Payload--;
            FinishedCount++;
            CommandConsumer.CommandHandled();
        }
    }

    // 处理command 的 consumer(随时可能是成为一个LinearCommandConsumer, 当一个linear command 发送给他时)
    public class CommandQueueConsumer
    {
        public ZmqSocket CommandSender { get; set; }
        public int Payload { get; set; }
        public double FinishedCount { get; set; }

        public CommandQueueConsumer(string pushEndPoint)
        {
            CommandSender = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.PUSH);
            CommandSender.Bind(pushEndPoint);
        }

        public void PushMessageContext(IMessageContext commandContext)
        {
            CommandSender.Send(commandContext.ToJson(), Encoding.UTF8);
            Payload++;
        }

        internal void CommandHandled()
        {
            Payload--;
            FinishedCount++;
        }
    }

    public class CommandDistributer : MessageConsumer, IMessageDistributor
    {
        protected Dictionary<object, LinearCommandConsumer> LinearCommandStates { get; set; }
        protected Dictionary<string, CommandState> CommandStateQueue = new Dictionary<string, CommandState>();
        protected List<CommandQueueConsumer> CommandConsumers { get; set; }
        protected ILinearCommandManager LinearCommandManager { get; set; }
        protected string ReceiveReplyEndPoint { get; set; }
        protected string[] PushEndPoints { get; set; }
        ZmqSocket ReplyReceiver { get; set; }

        public CommandDistributer(string replyToEndPoint, string receiveReplyEndPoint, string[] pushEndPoints,
                                 ILinearCommandManager linearCommandManager, params string[] pullEndPoints)
            : base(replyToEndPoint, pullEndPoints)
        {
            LinearCommandStates = new Dictionary<object, LinearCommandConsumer>();
            CommandConsumers = new List<CommandQueueConsumer>();
            LinearCommandManager = linearCommandManager;
            ReceiveReplyEndPoint = receiveReplyEndPoint;
            PushEndPoints = pushEndPoints;
        }

        protected override void OnMessageHandled(MessageReply reply)
        {
            lock (this)
            {
                CommandState commandState;
                if (CommandStateQueue.TryGetValue(reply.MessageID, out commandState))
                {
                    if (commandState.LinearCommandConsumer != null)
                    {
                        commandState.LinearCommandConsumer.CommandHandled();
                        if (commandState.LinearCommandConsumer.Payload == 0)
                        {
                            LinearCommandStates.TryRemove(commandState.LinearCommandConsumer.LinearKey);
                        }
                    }
                    else
                    {
                        commandState.CommandConsumer.CommandHandled();
                    }
                    CommandStateQueue.Remove(reply.MessageID);
                }
            }
            if (Replier != null)
            {
                Replier.Send(reply.ToJson(), Encoding.UTF8);
            }
            base.OnMessageHandled(reply);
        }

        public override void StartConsuming()
        {
            base.StartConsuming();
            PushEndPoints.ForEach(pushEndPoint =>
            {
                CommandConsumers.Add(new CommandQueueConsumer(pushEndPoint));
            });

            ReplyReceiver = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.PULL);
            ReplyReceiver.Bind(ReceiveReplyEndPoint);
            Task.Factory.StartNew(ReceiveCommandReplies);
        }

        void ReceiveCommandReplies()
        {
            while (true)
            {
                try
                {
                    var rcvdMsg = ReplyReceiver.Receive(Encoding.UTF8);
                    var reply = rcvdMsg.ToJsonObject<MessageReply>();
                    if (reply != null)
                    {
                        this.OnMessageHandled(reply);
                    }
                }
                catch (Exception e)
                {
                    Console.Write(e.GetBaseException().Message);
                }
            }
        }

        protected override void Consume(IMessageContext commandContext)
        {
            lock (this)
            {
                CommandQueueConsumer consumer;
                var commandState = new CommandState { CommandID = commandContext.MessageID };
                if (commandContext.Message is ILinearCommand)
                {
                    // 取出command的linear key, 作为consumer的选择索引
                    var linearKey = LinearCommandManager.GetLinearKey(commandContext.Message as ILinearCommand);
                    LinearCommandConsumer linearCommandConsumer;
                    // 尝试从字典中取出linearkey族command的当前consumer
                    if (!LinearCommandStates.TryGetValue(linearKey, out linearCommandConsumer))
                    {
                        // linearkey对应的consumer不存在,说明没有任何consumer在消费该linearkey族的command
                        // 此时选用负载最轻的consumer作为当前command的consumer, 并且被选中的consumer成为
                        // 该linearkey族command的LinearCommandConsumer, 并加入字典中.
                        consumer = CommandConsumers.OrderBy(c => c.Payload).FirstOrDefault();
                        linearCommandConsumer = new LinearCommandConsumer(consumer, linearKey);
                        LinearCommandStates.Add(linearKey, linearCommandConsumer);
                    }
                    else
                    {
                        // 根据linearKey, 从consumers中取出正在处理该linearkey族command的
                        // consumer作为当前command的消费者
                        consumer = linearCommandConsumer.CommandConsumer;
                    }
                    // 记录command的Consumer
                    commandState.LinearCommandConsumer = linearCommandConsumer;
                    // 将command 发给对应的 LinearCommandConsumer
                    linearCommandConsumer.PushCommandContext(commandContext);
                }
                else
                {
                    // 非linearcommand直接选择负载最轻的consumer 进行发送.
                    consumer = CommandConsumers.OrderBy(c => c.Payload).FirstOrDefault();
                    if (consumer != null)
                    {
                        // 将command 发给选中的consumer
                        consumer.PushMessageContext(commandContext);
                    }
                }
                // 记录command在哪个consumer上处理
                commandState.CommandConsumer = consumer;
                // 记录command的执行状态, 当reply到来时会取出并更新consumer的状态
                CommandStateQueue.Add(commandContext.MessageID, commandState);
            }
        }

        public override string GetStatus()
        {
            StringBuilder status = new StringBuilder();
            status.Append("Consumer status:<br>");
            lock (this)
            {
                CommandConsumers.ForEach(consumer =>
                {
                    status.AppendFormat("Consumer({0}):{1}/done:{2}<br>", consumer.CommandSender.LastEndpoint, consumer.Payload, consumer.FinishedCount);
                });

                status.Append("Linear Command Status: <br>");
                LinearCommandStates.ForEach(linearCommandState =>
                {
                    status.AppendFormat("LinearKey:{0}&nbsp;Consumer({1}):{2}/done:{3} <br>", linearCommandState.Key,
                        linearCommandState.Value.CommandConsumer.CommandSender.LastEndpoint,
                        linearCommandState.Value.Payload,
                        linearCommandState.Value.FinishedCount);
                });
            }
            return status.ToString();
        }
    }
}
