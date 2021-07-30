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
            gchannel.SetChannelInfo(new ChannelInfo() { Key = nguid, Name = generic }, string.Empty);
            activechannels.Add(generic, nguid);
            return base.OnActivateAsync();
        }

        public async Task<Guid> Add(string name, string password)
        {
            Guid guid;
            if (activechannels.ContainsKey(name))
                activechannels.TryGetValue(name, out guid);
            else
            {
                var nguid = Guid.NewGuid();
                var nchannel = this.GrainFactory.GetGrain<IChannel>(nguid);
                await nchannel.SetChannelInfo(new ChannelInfo() { Key = nguid, Name = name }, password);
                activechannels.Add(name, nguid);
                guid = nguid;
            }
            return guid;
        }

        public async Task<Guid> GetChannel(string name)
        {
            Guid guid = Guid.Empty;
            if (activechannels.ContainsKey(name))
                activechannels.TryGetValue(name, out guid);
            return await Task.FromResult(guid);
        }

        public async Task<ChannelInfo[]> GetAllChannels()
        {
            ChannelInfo[] info = Array.Empty<ChannelInfo>(); 
            if (activechannels.Count > 0)
                info = activechannels.Select(s => new ChannelInfo() { Key = s.Value, Name = s.Key }).ToArray();
            return await Task.FromResult(info);
        }

        public async Task<Guid> Remove(string name)
        {
            Guid guid = Guid.Empty;
            if (generic != name && activechannels.ContainsKey(name))
            {
                activechannels.TryGetValue(name, out guid);
                await Task.FromResult(activechannels.Remove(name));
            }
            return guid;
        }
    }
}