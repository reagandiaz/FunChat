using System;
using System.Threading.Tasks;

namespace FunChat.GrainIntefaces
{
    public interface IChannelRegistry : Orleans.IGrainWithGuidKey
    {
        Task<Guid> Add(string name, string password);
        Task<Guid> GetChannel(string name);
        Task<ChannelInfo[]> GetAllChannels();
        Task<Guid> Remove(string name);
        Task<ChannelInfo[]> UpdateMembership(UserInfo userinfo);
    }
}
