using IFramework.Command;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroMQ;
using IFramework.Infrastructure;
using IFramework.Message.Impl;
using System.Collections.Concurrent;
using IFramework.MessageQueue.ZeroMQ.MessageFormat;

namespace IFramework.MessageQueue.ZeroMQ
{
    // 记录了command 在哪个consumer中处理, 当command是linear的时候LinearCommandConsumer不为空
    // CommandConsumer表示command在哪个consumer上处理.
    public class CommandState
    {
        public string CommandID { get; set; }
        public string FromEndPoint { get; set; }
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

        public CommandQueueConsumer(ZmqSocket commandSender)
        {
            CommandSender = commandSender;
        }

        public void PushMessageContext(IMessageContext commandContext)
        {
            CommandSender.SendFrame(commandContext.GetFrame());
            Payload++;
        }

        internal void CommandHandled()
        {
            Payload--;
            FinishedCount++;
        }
    }


    public class CommandDistributor : MessageConsumer<Frame>, IMessageDistributor
    {
        protected Dictionary<object, LinearCommandConsumer> LinearCommandStates { get; set; }
        protected Dictionary<string, CommandState> CommandStateQueue = new Dictionary<string, CommandState>();
        protected List<CommandQueueConsumer> CommandConsumers { get; set; }
        protected string[] TargetEndPoints { get; set; }
       
        public CommandDistributor(string[] targetEndPoints)
            : this(null, targetEndPoints)
        {

        }

        public CommandDistributor(string receiveEndPoint,
                                  params string[] targetEndPoints)
            : base(receiveEndPoint)
        {
            LinearCommandStates = new Dictionary<object, LinearCommandConsumer>();
            CommandConsumers = new List<CommandQueueConsumer>();
            TargetEndPoints = targetEndPoints;
        }

        public override void Start()
        {
            base.Start();

            TargetEndPoints.ForEach(targetEndPoint =>
            {
                var commandSender = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.PUSH);
                commandSender.Connect(targetEndPoint);
                CommandConsumers.Add(new CommandQueueConsumer(commandSender));
            });
        }

        protected override void ReceiveMessage(Frame frame)
        {
            MessageQueue.Add(frame);
        }

        protected override void ConsumeMessage(Frame frame)
        {
            var messageCode = frame.GetMessageCode();
            if (messageCode == (short)MessageCode.Message)
            {
                var messageContext = frame.GetMessage<MessageContext>();
                if (messageContext != null)
                {
                    ConsumeMessageContext(messageContext);
                }
                else
                {   
                    //should never go here
                    _Logger.ErrorFormat("unknown frame! buflength: {1} message:{0}",
                                                           frame.GetMessage(), frame.BufferSize);
                }
            }
            else if (messageCode == (short)MessageCode.MessageHandledNotification)
            {
                var notification = frame.GetMessage<MessageHandledNotification>();
                if (notification != null)
                {
                    ConsumeHandledNotification(notification);
                }
            }
        }

        protected void ConsumeHandledNotification(IMessageHandledNotification notification)
        {
            _Logger.InfoFormat("Handle notification, commandID:{0}", notification.MessageID);
            CommandState commandState;
            if (CommandStateQueue.TryGetValue(notification.MessageID, out commandState))
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
                CommandStateQueue.Remove(notification.MessageID);
            }

            var notificationSender = GetReplySender(commandState.FromEndPoint);
            if (notificationSender != null)
            {
                notificationSender.SendFrame(new MessageHandledNotification(notification.MessageID)
                                                    .GetFrame());
                _Logger.InfoFormat("send notification, commandID:{0}", notification.MessageID);
            }
        }

        protected void ConsumeMessageContext(IMessageContext commandContext)
        {
            _Logger.InfoFormat("send to consumer, commandID:{0} payload:{1}",
                                         commandContext.MessageID, commandContext.ToJson());

            CommandQueueConsumer consumer;
            var commandState = new CommandState
            {
                CommandID = commandContext.MessageID,
                FromEndPoint = commandContext.FromEndPoint
            };
            if (!string.IsNullOrWhiteSpace(ReceiveEndPoint))
            {
                commandContext.FromEndPoint = this.ReceiveEndPoint;
            }

            if (!string.IsNullOrWhiteSpace(commandContext.Key))
            {
                // 取出command的linear key, 作为consumer的选择索引
                var linearKey = commandContext.Key;
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
