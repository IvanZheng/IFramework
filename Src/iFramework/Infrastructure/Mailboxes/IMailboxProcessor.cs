using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes
{
    public interface IMailboxProcessor
    {
        void Start();
        void Stop();
        Task Process(string key, Func<Task> process, TaskCompletionSource<object> taskCompletionSource = null);
        Task<T> Process<T>(string key, Func<Task<T>> process, TaskCompletionSource<object> taskCompletionSource = null);

        string Status { get; }
    }
}
