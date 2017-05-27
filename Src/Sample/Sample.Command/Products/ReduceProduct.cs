using System;
using IFramework.Command.Impl;

namespace Sample.Command
{
    public class ReduceProduct : LinearCommandBase
    {
        [LinearKey]
        public Guid ProductId { get; set; }

        public int ReduceCount { get; set; }
    }
}