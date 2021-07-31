using System;
using System.Threading.Tasks;

namespace FunChat.GrainIntefaces
{
    public interface IChannel : Orleans.IGrainWithGuidKey
    {
        Task SetChannelInfo(ChannelInfo channelinfo, string password);
        Task<string> Name();
        Task<ChannelInfo> Join(UserInfo userInfo, string password);
        Task<ChannelInfo> Leave(string username);
        Task<ChannelInfo> UpdateUserInfo(UserInfo userInfo);
        Task<bool> Message(UserInfo userinfo, Message msg);
        Task<string[]> GetMembers();
        Task ClearMembers();
        Task<Message[]> ReadHistory(int numberOfMessages);
    }
}
