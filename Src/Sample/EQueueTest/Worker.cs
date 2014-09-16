using IFramework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using System.Threading.Tasks;
using Sample.Command;

namespace EQueueTest
{
    public class Worker
    {
        ICommandBus _CommandBus;
        public Worker(ICommandBus commandBus)
        {
            _CommandBus = commandBus;
        }

        public ApiResult<TResult> ActionWithResult<TResult>(ICommand command)
        {
            return ExceptionManager.Process<TResult>(() =>
            {
                var task = _CommandBus.Send<TResult>(command);
                task.Wait();
                return task.Result;
            });
        }

        public ApiResult Action(ICommand command)
        {
            var commandGenericInterfaceType = command.GetType().GetInterfaces().FirstOrDefault(i => i.IsGenericType);
            if (commandGenericInterfaceType != null)
            {
                var resultType = commandGenericInterfaceType.GetGenericArguments().First();
                var result = this.InvokeGenericMethod(resultType, "ActionWithResult", new object[] { command })
                            as ApiResult;
                return result;
            }
            else
            {
                return ExceptionManager.Process(() =>
                {
                    _CommandBus.Send(command);//.Wait();
                });
            }
        }

        public void DoCommand(List<ICommand> batchCommands)
        {
            batchCommands.ForEach(cmd =>
            {
                var task = _CommandBus.Send<string>(cmd);
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Console.WriteLine(t.Exception.GetBaseException().Message);
                    }
                    else
                    {
                        Console.WriteLine(t.Result);
                    }
                });
            });
        }

        internal void StartTest(int batchCount)
        {
            Task.Factory.StartNew(() => {
                int i = 0;
                while (i++ < batchCount)
                {
                    var commands = new List<ICommand>();
                    commands.Add(new Login { UserName = "Ivan0", Password = "123456" });
                    commands.Add(new Login { UserName = "Ivan1", Password = "123456" });
                    commands.Add(new Login { UserName = "Ivan2", Password = "123456" });
                    commands.Add(new Login { UserName = "Ivan3", Password = "123456" });
                    DoCommand(commands);
                }
            });
          
        }
    }
}
