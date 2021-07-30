using Orleans.Concurrency;
using System;

namespace FunChat.GrainIntefaces
{
    [Immutable]
    public class UserInfo
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
