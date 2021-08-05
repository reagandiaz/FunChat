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

        public async Task<ChannelInfoResult> Add(string name, string password)
        {
            ChannelInfoResult result = new ChannelInfoResult();
            try
            {
                Guid guid;

                if (activechannels.ContainsKey(name))
                {
                    activechannels.TryGetValue(name, out guid);
                    result.State = ResultState.Failed;
                    result.Info = new ChannelInfo() { Key = guid, Name = name };
                }
                else
                {
                    var nguid = Guid.NewGuid();
                    var nchannel = this.GrainFactory.GetGrain<IChannel>(nguid);
                    await nchannel.Initialize(new ChannelInfo() { Key = nguid, Name = name }, password);
                    activechannels.Add(name, nguid);

                    result.State = ResultState.Success;
                    result.Info = new ChannelInfo() { Key = guid, Name = name };
                }
            }
            catch
            {
                result.State = ResultState.Error;
            }
            return result;
        }

        public async Task<ChannelInfoResult> GetChannel(string name)
        {
            ChannelInfoResult result = new ChannelInfoResult();

            Guid guid = Guid.Empty;
            if (activechannels.ContainsKey(name))
                activechannels.TryGetValue(name, out guid);

            if (guid == Guid.Empty)
                result.State = ResultState.Failed;
            else
            {
                result.State = ResultState.Success;
                result.Info = new ChannelInfo() { Key = guid, Name = name };
            }

            return await Task.FromResult(result);
        }

        public async Task<ChannelInfoListResult> GetAllChannels()
        {
            ChannelInfoListResult result = new ChannelInfoListResult();
            if (activechannels.Count > 0)
                result.Infos = activechannels.Select(s => new ChannelInfo() { Key = s.Value, Name = s.Key }).ToArray();
            result.State = ResultState.Success;
            return await Task.FromResult(result);
        }

        public async Task<ChannelInfoResult> Remove(string name)
        {
            ChannelInfoResult result = new ChannelInfoResult();
            if (generic != name && activechannels.ContainsKey(name))
            {
                activechannels.TryGetValue(name, out Guid guid);
                if (guid != Guid.Empty)
                {
                    activechannels.Remove(name);
                    result.State = ResultState.Success;
                    result.Info = new ChannelInfo() { Key = guid, Name = name };
                }
            }
            return await Task.FromResult(result);
        }

        public async Task<ChannelInfoListResult> UpdateMembership(UserInfo userinfo)
        {
            ChannelInfoListResult result = new ChannelInfoListResult();
            try
            {
                List<ChannelInfo> channels = new List<ChannelInfo>();
                const int maxchannel = 3;
                int ctr = 0;
                foreach (var item in activechannels)
                {
                    var channel = this.GrainFactory.GetGrain<IChannel>(item.Value);
                    var channelinforesult = await channel.UpdateUserInfo(userinfo);
                    if (channelinforesult.State == ResultState.Success && channelinforesult.Info.Key != Guid.Empty)
                    {
                        ctr++;
                        channels.Add(channelinforesult.Info);
                    }
                    if (ctr == maxchannel)
                        break;
                }
                result.State = ResultState.Success;
                result.Infos = channels.ToArray();
            }
            catch
            {
                result.State = ResultState.Error;
            }
            return result;
        }
    }
}