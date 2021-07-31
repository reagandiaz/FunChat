using Orleans.Concurrency;
using System;
using System.Collections.Generic;

namespace FunChat.GrainIntefaces
{
    [Immutable]
    public class ChannelInfo
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
