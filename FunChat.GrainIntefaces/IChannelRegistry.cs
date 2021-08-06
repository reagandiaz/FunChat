using System;
using System.Threading.Tasks;

namespace FunChat.GrainIntefaces
{
    public interface IChannelRegistry : Orleans.IGrainWithGuidKey
    {  
        Task<Result<ChannelInfo>> Add(string name, string password);
        Task<Result<ChannelInfo>> GetChannel(string name);
        Task<Result<ChannelInfo[]>> GetAllChannels();
        Task<Result<ChannelInfo>> Remove(string name);
        Task<Result<ChannelInfo[]>> UpdateMembership(UserInfo userinfo);
    }
}
