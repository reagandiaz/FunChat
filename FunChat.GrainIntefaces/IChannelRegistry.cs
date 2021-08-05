using System;
using System.Threading.Tasks;

namespace FunChat.GrainIntefaces
{
    public interface IChannelRegistry : Orleans.IGrainWithGuidKey
    {  
        Task<ChannelInfoResult> Add(string name, string password);
        Task<ChannelInfoResult> GetChannel(string name);
        Task<ChannelInfoListResult> GetAllChannels();
        Task<ChannelInfoResult> Remove(string name);
        Task<ChannelInfoListResult> UpdateMembership(UserInfo userinfo);
    }
}
