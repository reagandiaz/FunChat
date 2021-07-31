using FunChat.GrainIntefaces;
using FunChat.Grains.Tools;
using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FunChat.Grains
{
    public class UserGrain : Orleans.Grain, IUser
    {
        UserInfo userInfo;
        bool isadmin;
        readonly int[] loginlimit = new int[] { 3, 10 };
        readonly int[] channellimit = new int[] { 6, 18 };
        const int channelidlimit = 6;
        const string generic = "generic";
        const int maxchannel = 2;

        readonly List<string> currentchannel = new List<string>();

        public async Task<Guid> Login(string username, string password)
        {
            Guid guid = Guid.Empty;
            if (username == password && (new NameValidator(username)).IsValid(loginlimit[0], loginlimit[1]))
            {
                userInfo = new UserInfo() { Name = username, Key = this.GetGrainIdentity().PrimaryKey };
                isadmin = username == "Admin";
                var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);

                //update state
                var membership = await channelregistry.UpdateMembership(userInfo);
                for (int i = 0; i < membership.Length; i++)
                {
                    if (membership[i].Name != generic)
                        currentchannel.Add(membership[i].Name);
                }
                //join generic chanel
                await JoinChannel(generic, String.Empty);

                guid = userInfo.Key;
            }
            return guid;
        }

        public async Task<Guid> LocateChannel(string channel)
        {
            Guid guid = Guid.Empty;
            if (userInfo != null)
            {
                var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(guid);
                guid = (await channelregistry.GetChannel(channel));
            }
            return guid;
        }
        public async Task<ChannelInfo> CreateChannel(string password)
        {
            Guid guid = Guid.Empty;
            string channelname = string.Empty;
            if (userInfo != null && (new NameValidator(password)).IsValid(channellimit[0], channellimit[1]))
            {
                var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);

                channelname = Guid.NewGuid().ToString("n").Substring(0, channelidlimit);

                guid = (await channelregistry.Add(channelname, password));
            }
            return new ChannelInfo() { Key = guid, Name = channelname };
        }


        public async Task<ChannelInfo> JoinChannel(string channel, string password)
        {
            ChannelInfo channelInfo = new ChannelInfo() { Key = Guid.Empty, Name = string.Empty };
            if (userInfo != null)
            {
                if (channel == generic || (currentchannel.Count < maxchannel && (new NameValidator(channel)).IsValid(channellimit[0], channellimit[1])))
                {
                    var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                    var guid = (await channelregistry.GetChannel(channel));
                    var cchannel = this.GrainFactory.GetGrain<IChannel>(guid);
                    channelInfo = await cchannel.Join(userInfo, password);
                    //iterate if not generic
                    if (channelInfo.Name != generic && channelInfo.Name != String.Empty)
                    {
                        if (!currentchannel.Contains(channelInfo.Name))
                            currentchannel.Add(channelInfo.Name);
                    }
                }
            }
            return channelInfo;
        }

        public async Task<ChannelInfo> LeaveChannelByName(string channel)
        {
            ChannelInfo channelInfo = new ChannelInfo() { Key = Guid.Empty, Name = string.Empty };
            if (userInfo != null)
            {
                var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                var guid = await channelregistry.GetChannel(channel);

                if (guid != Guid.Empty)
                    channelInfo = await this.LeaveChannelByKey(guid);
            }
            return channelInfo;
        }

        public async Task<ChannelInfo> LeaveChannelByKey(Guid channelguid)
        {
            ChannelInfo channelInfo = new ChannelInfo() { Key = Guid.Empty, Name = string.Empty };
            if (userInfo != null)
            {
                var channel = this.GrainFactory.GetGrain<IChannel>(channelguid);
                //cannot leave default channel
                if (await channel.Name() != generic)
                {
                    channelInfo = await channel.Leave(userInfo.Name);
                    if (channelInfo.Name != String.Empty)
                    {
                        if (currentchannel.Contains(channelInfo.Name))
                            currentchannel.Remove(channelInfo.Name);
                    }
                }
            }
            return channelInfo;
        }

        public async Task<ChannelInfo[]> GetAllChannels()
        {
            ChannelInfo[] channelinfo = Array.Empty<ChannelInfo>();
            if (isadmin)
            {
                var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                channelinfo = await channelregistry.GetAllChannels();
            }
            return channelinfo;
        }

        public async Task<string[]> GetChannelMembers(string channelname)
        {
            string[] members = Array.Empty<string>();
            if (isadmin || currentchannel.Contains(channelname) || channelname == generic)
            {
                var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                var guid = await channelregistry.GetChannel(channelname);
                if (guid != Guid.Empty)
                {
                    var channel = this.GrainFactory.GetGrain<IChannel>(guid);
                    members = await channel.GetMembers();
                }
            }
            return members;
        }


        public async Task<Guid> RemoveChannel(string channelname)
        {
            Guid guid = Guid.Empty;
            if (isadmin)
            {
                var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                guid = await channelregistry.Remove(channelname);
                if (guid != Guid.Empty)
                {
                    var channel = this.GrainFactory.GetGrain<IChannel>(guid);
                    await channel.ClearMembers();
                }
            }
            return guid;
        }
    }
}
