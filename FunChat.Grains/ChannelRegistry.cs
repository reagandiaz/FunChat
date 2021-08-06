using FunChat.GrainIntefaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FunChat.Grains
{
    public class ChannelRegistry : Orleans.Grain, IChannelRegistry
    {
        readonly Dictionary<string, Guid> activechannels = new Dictionary<string, Guid>();

        const string generic = "generic";

        public override Task OnActivateAsync()
        {
            Guid nguid = Guid.NewGuid();
            var gchannel = this.GrainFactory.GetGrain<IChannel>(nguid);
            gchannel.Initialize(new ChannelInfo() { Key = nguid, Name = generic }, string.Empty);
            activechannels.Add(generic, nguid);
            return base.OnActivateAsync();
        }

        public async Task<Result<ChannelInfo>> Add(string name, string password)
        {
            Result<ChannelInfo> result = new Result<ChannelInfo>();
            try
            {
                Guid guid;

                if (activechannels.ContainsKey(name))
                {
                    activechannels.TryGetValue(name, out guid);
                    result.Success = false;
                    result.Info = new ChannelInfo() { Key = guid, Name = name };
                }
                else
                {
                    var nguid = Guid.NewGuid();
                    var nchannel = this.GrainFactory.GetGrain<IChannel>(nguid);
                    await nchannel.Initialize(new ChannelInfo() { Key = nguid, Name = name }, password);
                    activechannels.Add(name, nguid);

                    result.Success = true;
                    result.Info = new ChannelInfo() { Key = guid, Name = name };
                }
            }
            catch
            {
                result.Success = false;
            }
            return result;
        }

        public async Task<Result<ChannelInfo>> GetChannel(string name)
        {
            Result<ChannelInfo> result = new Result<ChannelInfo>();

            Guid guid = Guid.Empty;
            if (activechannels.ContainsKey(name))
                activechannels.TryGetValue(name, out guid);

            if (guid == Guid.Empty)
                result.Success = false;
            else
            {
                result.Success = true;
                result.Info = new ChannelInfo() { Key = guid, Name = name };
            }

            return await Task.FromResult(result);
        }

        public async Task<Result<ChannelInfo[]>> GetAllChannels()
        {
            Result<ChannelInfo[]> result = new Result<ChannelInfo[]>();
            if (activechannels.Count > 0)
                result.Info = activechannels.Select(s => new ChannelInfo() { Key = s.Value, Name = s.Key }).ToArray();
            result.Success = true;
            return await Task.FromResult(result);
        }

        public async Task<Result<ChannelInfo>> Remove(string name)
        {
            Result<ChannelInfo> result = new Result<ChannelInfo>();
            if (generic != name && activechannels.ContainsKey(name))
            {
                activechannels.TryGetValue(name, out Guid guid);
                if (guid != Guid.Empty)
                {
                    activechannels.Remove(name);
                    result.Success = true;
                    result.Info = new ChannelInfo() { Key = guid, Name = name };
                }
            }
            return await Task.FromResult(result);
        }

        public async Task<Result<ChannelInfo[]>> UpdateMembership(UserInfo userinfo)
        {
            Result<ChannelInfo[]> result = new Result<ChannelInfo[]>();
            try
            {
                List<ChannelInfo> channels = new List<ChannelInfo>();
                const int maxchannel = 3;
                int ctr = 0;
                foreach (var item in activechannels)
                {
                    var channel = this.GrainFactory.GetGrain<IChannel>(item.Value);
                    var channelinforesult = await channel.UpdateUserInfo(userinfo);
                    if (channelinforesult.Success == true && channelinforesult.Info.Key != Guid.Empty)
                    {
                        ctr++;
                        channels.Add(channelinforesult.Info);
                    }
                    if (ctr == maxchannel)
                        break;
                }
                result.Success = true;
                result.Info = channels.ToArray();
            }
            catch
            {
                result.Success = false;
            }
            return result;
        }
    }
}