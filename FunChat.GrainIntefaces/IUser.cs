using System;
using System.Threading.Tasks;

namespace FunChat.GrainIntefaces
{
    public interface IUser : Orleans.IGrainWithGuidKey
    {
        Task<UserInfoResult> Login(string username, string password);
        Task<CurrentChannelsResult> CurrentChannels();
        Task<ChannelInfoResult> LocateChannel(string channel);
        Task<ChannelInfoResult> CreateChannel(string password);
        Task<ChannelInfoResult> JoinChannel(string channel, string password);
        Task<ChannelInfoResult> LeaveChannelByName(string channel);
        Task<ChannelInfoResult> LeaveChannelByKey(Guid channelguid);
        Task<ChannelInfoListResult> GetAllChannels();
        Task<MembersResult> GetChannelMembers(string channelname);
        Task<ChannelInfoResult> RemoveChannel(string channelname);
    }
}
