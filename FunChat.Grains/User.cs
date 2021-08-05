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
        const string admin = "Admin";

        readonly List<string> currentchannel = new List<string>();

        public async Task<UserInfoResult> Login(string username, string password)
        {
            UserInfoResult result = new UserInfoResult();
            try
            {
                if (username == password && (new NameValidator(username)).IsValid(loginlimit[0], loginlimit[1]))
                {
                    userInfo = new UserInfo() { Name = username, Key = this.GetGrainIdentity().PrimaryKey };
                    isadmin = username == admin;
                    var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);

                    //update state
                    var membershipresult = await channelregistry.UpdateMembership(userInfo);
                    if (membershipresult.State == ResultState.Success)
                    {
                        for (int i = 0; i < membershipresult.Infos.Length; i++)
                        {
                            if (membershipresult.Infos[i].Name != generic)
                                currentchannel.Add(membershipresult.Infos[i].Name);
                        }
                        //join generic chanel
                        var joinresult = await JoinChannel(generic, String.Empty);
                        if (joinresult.State == ResultState.Success)
                        {
                            result.State = ResultState.Success;
                            result.Info = userInfo;
                        }
                    }
                }
            }
            catch
            {
                result.State = ResultState.Error;
            }
            return result;
        }

        public async Task<CurrentChannelsResult> CurrentChannels()
        {
            CurrentChannelsResult result = new CurrentChannelsResult();
            try
            {
                if (userInfo != null)
                {
                    result.Items = currentchannel.ToArray();
                    result.State = ResultState.Success;
                }
            }
            catch
            {
                result.State = ResultState.Error;
            }
            return await Task.FromResult(result);
        }

        public async Task<ChannelInfoResult> LocateChannel(string channel)
        {
            ChannelInfoResult result = new ChannelInfoResult();
            try
            {
                if (userInfo != null)
                {
                    var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                    result = await channelregistry.GetChannel(channel);
                }
            }
            catch
            {
                result.State = ResultState.Error;
            }
            return result;
        }

        public async Task<ChannelInfoResult> CreateChannel(string password)
        {
            ChannelInfoResult result = new ChannelInfoResult();
            try
            {
                if (userInfo != null && (new NameValidator(password)).IsValid(channellimit[0], channellimit[1]))
                {
                    var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                    var channelname = Guid.NewGuid().ToString("n").Substring(0, channelidlimit);
                    result = (await channelregistry.Add(channelname, password));
                }
            }
            catch
            {
                result.State = ResultState.Error;
            }
            return result;
        }

        public async Task<ChannelInfoResult> JoinChannel(string channel, string password)
        {
            ChannelInfoResult result = new ChannelInfoResult();
            try
            {
                if (userInfo != null)
                {
                    if (channel == generic || (currentchannel.Count < maxchannel && (new NameValidator(channel)).IsValid(channellimit[0], channellimit[1])))
                    {
                        var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                        result = (await channelregistry.GetChannel(channel));
                        //check if found in registry
                        if (result.State == ResultState.Success && result.Info.Key != Guid.Empty)
                        {
                            var cchannel = this.GrainFactory.GetGrain<IChannel>(result.Info.Key);
                            result = await cchannel.Join(userInfo, password);
                            //join suceess
                            if (result.State == ResultState.Success)
                            {
                                //iterate if not generic
                                if (result.Info.Name != generic && result.Info.Name != String.Empty)
                                {
                                    if (!currentchannel.Contains(result.Info.Name))
                                        currentchannel.Add(result.Info.Name);
                                }
                            }
                        }
                        else
                            result.State = ResultState.Failed;
                    }
                }
            }
            catch
            {
                result.State = ResultState.Error;
            }
            return result;
        }

        public async Task<ChannelInfoResult> LeaveChannelByName(string channel)
        {
            ChannelInfoResult result = new ChannelInfoResult();
            try
            {
                if (userInfo != null)
                {
                    var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                    result = await channelregistry.GetChannel(channel);

                    if (result.State == ResultState.Success && result.Info.Key != Guid.Empty)
                        result = await this.LeaveChannelByKey(result.Info.Key);
                    else
                        result.State = ResultState.Failed;
                }
            }
            catch
            {
                result.State = ResultState.Error;
            }
            return result;
        }

        public async Task<ChannelInfoResult> LeaveChannelByKey(Guid channelguid)
        {
            ChannelInfoResult result = new ChannelInfoResult();
            try
            {
                if (userInfo != null)
                {
                    var channel = this.GrainFactory.GetGrain<IChannel>(channelguid);
                    //cannot leave default channel
                    if (await channel.Name() != generic)
                    {
                        result = await channel.Leave(userInfo.Name);
                        if (result.State == ResultState.Success && result.Info.Name != String.Empty)
                        {
                            if (currentchannel.Contains(result.Info.Name))
                                currentchannel.Remove(result.Info.Name);
                        }
                    }
                }
            }
            catch
            {
                result.State = ResultState.Error;
            }
            return result;
        }

        public async Task<ChannelInfoListResult> GetAllChannels()
        {
            ChannelInfoListResult result = new ChannelInfoListResult();
            try
            {
                if (isadmin)
                {
                    var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                    result = await channelregistry.GetAllChannels();
                }
                else
                    result.State = ResultState.Failed;
            }
            catch
            {
                result.State = ResultState.Error;
            }
            return result;
        }

        public async Task<MembersResult> GetChannelMembers(string channelname)
        {
            MembersResult result = new MembersResult();
            try
            {
                if (isadmin || currentchannel.Contains(channelname) || channelname == generic)
                {
                    var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                    var channelInfoResult = await channelregistry.GetChannel(channelname);
                    if (channelInfoResult.State == ResultState.Success)
                    {
                        if (channelInfoResult.Info.Key != Guid.Empty)
                        {
                            var channel = this.GrainFactory.GetGrain<IChannel>(channelInfoResult.Info.Key);
                            result = await channel.GetMembers();
                        }
                    }
                }
            }
            catch
            {
                result.State = ResultState.Error;
            }
            return result;
        }

        public async Task<ChannelInfoResult> RemoveChannel(string channelname)
        {
            ChannelInfoResult result = new ChannelInfoResult();
            try
            {
                if (isadmin)
                {
                    var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                    result = await channelregistry.Remove(channelname);
                    if (result.State == ResultState.Success)
                    {
                        var channel = this.GrainFactory.GetGrain<IChannel>(result.Info.Key);
                        await channel.ClearMembers();
                    }
                }
            }
            catch
            {
                result.State = ResultState.Error;
            }
            return result;
        }
    }
}
