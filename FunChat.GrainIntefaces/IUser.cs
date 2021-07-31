using System;
using System.Threading.Tasks;

namespace FunChat.GrainIntefaces
{
    public interface IUser : Orleans.IGrainWithGuidKey
    {
        Task<Guid> Login(string username, string password);
        Task<Guid> LocateChannel(string channel);
        Task<ChannelInfo> CreateChannel(string password);
        Task<ChannelInfo> JoinChannel(string channel, string password);
        Task<ChannelInfo> LeaveChannelByName(string channel);
        Task<ChannelInfo> LeaveChannelByKey(Guid channelguid);
        Task<ChannelInfo[]> GetAllChannels();
        Task<string[]> GetChannelMembers(string channelname);
        Task<Guid> RemoveChannel(string channelname);
    }
}
