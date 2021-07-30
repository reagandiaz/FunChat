using FunChat.GrainIntefaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FunChat.Grains
{
    public class Channel : Orleans.Grain, IChannel
    {
        ChannelInfo channelinfo;
        readonly List<string> members = new List<string>();
        readonly Queue<Message> messages = new Queue<Message>();
        const int buffer = 100;
        string password = string.Empty;

        public async Task SetChannelInfo(ChannelInfo channelinfo, string password)
        {
            this.channelinfo = new ChannelInfo() { Key = channelinfo.Key, Name = channelinfo.Name };
            this.password = password;
            await Task.CompletedTask;
        }
        public async Task<string> Name()
        {
            return await Task.FromResult(channelinfo.Name);
        }
        public async Task<ChannelInfo> Join(string username, string password)
        {
            ChannelInfo channelinfo = new ChannelInfo() { Name = string.Empty, Key = Guid.Empty };
            if (this.password == password && !members.Contains(username))
            {
                members.Add(username);
                channelinfo = new ChannelInfo() { Name = this.channelinfo.Name, Key = this.channelinfo.Key };
            }
            return await Task.FromResult(channelinfo);
        }
        public async Task<ChannelInfo> Leave(string username)
        {
            ChannelInfo channelinfo = new ChannelInfo() { Name = string.Empty, Key = Guid.Empty };
            if (members.Contains(username))
            {
                members.Remove(username);
                channelinfo = new ChannelInfo() { Name = this.channelinfo.Name, Key = this.channelinfo.Key };
            }
            return await Task.FromResult(channelinfo);
        }
        public async Task<bool> Message(Message msg)
        {
            bool iswritten = false;
            if (members.Contains(msg.Author))
            {
                msg.Channel = channelinfo.Name;
                if (messages.Count == buffer)
                    messages.Dequeue();
                messages.Enqueue(msg);
                iswritten = true;
            }
            return await Task.FromResult(iswritten);
        }

        public async Task<string[]> GetMembers()
        {
            return await Task.FromResult(members.ToArray());
        }

        public async Task ClearMembers()
        {
            members.Clear();
            await Task.CompletedTask;
        }

        public Task<Message[]> ReadHistory(int numberOfMessages)
        {
            Message[] response;
            if (numberOfMessages >= messages.Count)
                response = messages.ToArray();
            else
                response = messages.Skip(messages.Count - numberOfMessages).ToArray();

            return Task.FromResult(response);
        }
    }
}
