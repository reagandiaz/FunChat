using System;
using System.Threading.Tasks;

namespace FunChat.GrainIntefaces
{
    public interface IUser : Orleans.IGrainWithGuidKey
    {
        Task<Result<UserInfo>> Login(string username, string password);
        Task<Result<string[]>> CurrentChannels();
        Task<Result<ChannelInfo>> LocateChannel(string channel);
        Task<Result<ChannelInfo>> CreateChannel(string password);
        Task<Result<ChannelInfo>> JoinChannel(string channel, string password);
        Task<Result<ChannelInfo>> LeaveChannelByName(string channel);
        Task<Result<ChannelInfo>> LeaveChannelByKey(Guid channelguid);
        Task<Result<ChannelInfo[]>> GetAllChannels();
        Task<Result<string[]>> GetChannelMembers(string channelname);
        Task<Result<ChannelInfo>> RemoveChannel(string channelname);
    }
}
