using FunChat.GrainIntefaces;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FunChat.Grains
{
    public class Channel : Orleans.Grain, IChannel
    {
        ChannelInfo channelinfo;
        readonly Dictionary<string, Guid> members = new Dictionary<string, Guid>();
        readonly Queue<Message> messages = new Queue<Message>();
        const int buffer = 100;
        string password = string.Empty;
        IAsyncStream<Message> stream;
        const string providername = "FunChat";

        public async Task Initialize(ChannelInfo channelinfo, string password)
        {
            this.channelinfo = new ChannelInfo() { Key = channelinfo.Key, Name = channelinfo.Name };
            this.password = password;
            var streamProvider = GetStreamProvider(providername);
            stream = streamProvider.GetStream<Message>(channelinfo.Key, channelinfo.Name);
            await Task.CompletedTask;
        }
        public async Task<string> Name()
        {
            return await Task.FromResult(channelinfo.Name);
        }
        public async Task<bool> Message(UserInfo userinfo, Message msg)
        {
            bool iswritten = false;
            //validation
            if (members.ContainsKey(userinfo.Name) && members[userinfo.Name] == userinfo.Key)
            {
                msg.Author = userinfo.Name;
                msg.Channel = channelinfo.Name;
                if (messages.Count == buffer)
                    messages.Dequeue();
                messages.Enqueue(msg);

                await stream.OnNextAsync(msg);

                iswritten = true;
            }
            return await Task.FromResult(iswritten);
        }

        public async Task<Result<ChannelInfo>> Join(UserInfo userInfo, string password)
        {
            Result<ChannelInfo> result = new Result<ChannelInfo>();
            if (this.password == password)
            {
                if (members.ContainsKey(userInfo.Name))
                {
                    if (members[userInfo.Name] != userInfo.Key)
                        members[userInfo.Name] = userInfo.Key;
                }
                else
                    members.Add(userInfo.Name, userInfo.Key);

                result.Success = true;
                result.Info = new ChannelInfo() { Name = this.channelinfo.Name, Key = this.channelinfo.Key };
            }
            return await Task.FromResult(result);
        }

        public async Task<Result<ChannelInfo>> Leave(string username)
        {
            Result<ChannelInfo> result = new Result<ChannelInfo>();
            if (members.ContainsKey(username))
            {
                members.Remove(username);

                result.Success = true;
                result.Info = new ChannelInfo() { Name = this.channelinfo.Name, Key = this.channelinfo.Key };
            }
            return await Task.FromResult(result);
        }

        public async Task<Result<ChannelInfo>> UpdateUserInfo(UserInfo userInfo)
        {
            Result<ChannelInfo> result = new Result<ChannelInfo>();
            if (members.ContainsKey(userInfo.Name))
            {
                if (members[userInfo.Name] != userInfo.Key)
                    members[userInfo.Name] = userInfo.Key;

                result.Success = true;
                result.Info = new ChannelInfo() { Name = this.channelinfo.Name, Key = this.channelinfo.Key };
            }
            return await Task.FromResult(result);
        }

        public async Task<Result<Message[]>> ReadHistory()
        {
            Result<Message[]> result = new Result<Message[]>();
            if (buffer >= messages.Count)
                result.Info = messages.ToArray();
            else
                result.Info = messages.Skip(messages.Count - buffer).ToArray();

            result.Success = true;

            return await Task.FromResult(result);
        }

        public async Task<Result<string[]>> GetMembers()
        {
            return await Task.FromResult(new Result<string[]>() { Success = true, Info = members.Keys.ToArray() });
        }

        public async Task ClearMembers()
        {
            foreach (var item in members)
            {
                var user = this.GrainFactory.GetGrain<IUser>(item.Value);
                await user.LeaveChannelByKey(this.channelinfo.Key);
            }

            var handles = await stream.GetAllSubscriptionHandles();

            //loop thru subscription to unsubscribe
            for (int i = 0; i < handles.Count; i++)
                await handles[i].UnsubscribeAsync();

            members.Clear();
            await Task.CompletedTask;
        }
    }
}
