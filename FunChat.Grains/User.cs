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

        public async Task<Result<UserInfo>> Login(string username, string password)
        {
            Result<UserInfo> result = new Result<UserInfo>();
            try
            {
                if (username == password && (new NameValidator(username)).IsValid(loginlimit[0], loginlimit[1]))
                {
                    userInfo = new UserInfo() { Name = username, Key = this.GetGrainIdentity().PrimaryKey };
                    isadmin = username == admin;
                    var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);

                    //update state
                    var membershipresult = await channelregistry.UpdateMembership(userInfo);
                    if (membershipresult.Success == true)
                    {
                        for (int i = 0; i < membershipresult.Info.Length; i++)
                        {
                            if (membershipresult.Info[i].Name != generic)
                                currentchannel.Add(membershipresult.Info[i].Name);
                        }
                        //join generic chanel
                        var joinresult = await JoinChannel(generic, String.Empty);
                        if (joinresult.Success == true)
                        {
                            result.Success = true;
                            result.Info = userInfo;
                        }
                    }
                }
            }
            catch
            {
                result.Success = false;
            }
            return result;
        }

        public async Task<Result<string[]>> CurrentChannels()
        {
            Result<string[]> result = new Result<string[]>();
            try
            {
                if (userInfo != null)
                {
                    result.Info = currentchannel.ToArray();
                    result.Success = true;
                }
            }
            catch
            {
                result.Success = false;
            }
            return await Task.FromResult(result);
        }

        public async Task<Result<ChannelInfo>> LocateChannel(string channel)
        {
            Result<ChannelInfo> result = new Result<ChannelInfo>();
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
                result.Success = false;
            }
            return result;
        }

        public async Task<Result<ChannelInfo>> CreateChannel(string password)
        {
            Result<ChannelInfo> result = new Result<ChannelInfo>();
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
                result.Success = false;
            }
            return result;
        }

        public async Task<Result<ChannelInfo>> JoinChannel(string channel, string password)
        {
            Result<ChannelInfo> result = new Result<ChannelInfo>();
            try
            {
                if (userInfo != null)
                {
                    if (channel == generic || (currentchannel.Count < maxchannel && (new NameValidator(channel)).IsValid(channellimit[0], channellimit[1])))
                    {
                        var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                        result = (await channelregistry.GetChannel(channel));
                        //check if found in registry
                        if (result.Success == true && result.Info.Key != Guid.Empty)
                        {
                            var cchannel = this.GrainFactory.GetGrain<IChannel>(result.Info.Key);
                            result = await cchannel.Join(userInfo, password);
                            //join suceess
                            if (result.Success == true)
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
                            result.Success = false;
                    }
                }
            }
            catch
            {
                result.Success = false;
            }
            return result;
        }

        public async Task<Result<ChannelInfo>> LeaveChannelByName(string channel)
        {
            Result<ChannelInfo> result = new Result<ChannelInfo>();
            try
            {
                if (userInfo != null)
                {
                    var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                    result = await channelregistry.GetChannel(channel);

                    if (result.Success == true && result.Info.Key != Guid.Empty)
                        result = await this.LeaveChannelByKey(result.Info.Key);
                    else
                        result.Success = false;
                }
            }
            catch
            {
                result.Success = false;
            }
            return result;
        }

        public async Task<Result<ChannelInfo>> LeaveChannelByKey(Guid channelguid)
        {
            Result<ChannelInfo> result = new Result<ChannelInfo>();
            try
            {
                if (userInfo != null)
                {
                    var channel = this.GrainFactory.GetGrain<IChannel>(channelguid);
                    //cannot leave default channel
                    if (await channel.Name() != generic)
                    {
                        result = await channel.Leave(userInfo.Name);
                        if (result.Success == true && result.Info.Name != String.Empty)
                        {
                            if (currentchannel.Contains(result.Info.Name))
                                currentchannel.Remove(result.Info.Name);
                        }
                    }
                }
            }
            catch
            {
                result.Success = false;
            }
            return result;
        }

        public async Task<Result<ChannelInfo[]>> GetAllChannels()
        {
            Result<ChannelInfo[]> result = new Result<ChannelInfo[]>();
            try
            {
                if (isadmin)
                {
                    var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                    result = await channelregistry.GetAllChannels();
                }
                else
                    result.Success = false;
            }
            catch
            {
                result.Success = false;
            }
            return result;
        }

        public async Task<Result<string[]>> GetChannelMembers(string channelname)
        {
            Result<string[]> result = new Result<string[]>();
            try
            {
                if (isadmin || currentchannel.Contains(channelname) || channelname == generic)
                {
                    var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                    var channelInfoResult = await channelregistry.GetChannel(channelname);
                    if (channelInfoResult.Success == true)
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
                result.Success = false;
            }
            return result;
        }

        public async Task<Result<ChannelInfo>> RemoveChannel(string channelname)
        {
            Result<ChannelInfo> result = new Result<ChannelInfo>();
            try
            {
                if (isadmin)
                {
                    var channelregistry = this.GrainFactory.GetGrain<IChannelRegistry>(Guid.Empty);
                    result = await channelregistry.Remove(channelname);
                    if (result.Success == true)
                    {
                        var channel = this.GrainFactory.GetGrain<IChannel>(result.Info.Key);
                        await channel.ClearMembers();
                    }
                }
            }
            catch
            {
                result.Success = false;
            }
            return result;
        }
    }
}
