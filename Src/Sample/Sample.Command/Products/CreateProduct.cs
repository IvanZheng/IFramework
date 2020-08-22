﻿using System;

namespace Sample.Command
{
    public class CreateProduct : SerialCommandBase
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }
}