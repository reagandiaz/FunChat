using FunChat.GrainIntefaces;
using Orleans;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FunChat.Client
{
    public class ClientState
    {
        public IClusterClient Client;
        public IUser User;
        public string UserName;
        public Guid Key;
        public Dictionary<string, IChannel> Channels = new Dictionary<string, IChannel>();
        public Dictionary<string, StreamSubscriptionHandle<Message>> Subscriptions = new Dictionary<string, StreamSubscriptionHandle<Message>>();
        public async Task Clear()
        {
            User = null;
            UserName = String.Empty;
            Key = Guid.Empty;
            Channels.Clear();

            foreach (var item in Subscriptions)
                await item.Value.UnsubscribeAsync();

            Subscriptions.Clear();
        }
    }
}
