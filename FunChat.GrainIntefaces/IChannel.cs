using System;
using System.Threading.Tasks;

namespace FunChat.GrainIntefaces
{
    public interface IChannel : Orleans.IGrainWithGuidKey
    {
        Task Initialize(ChannelInfo channelinfo, string password);
        Task<string> Name();
        Task<ChannelInfoResult> Join(UserInfo userInfo, string password);
        Task<ChannelInfoResult> Leave(string username);
        Task<ChannelInfoResult> UpdateUserInfo(UserInfo userInfo);
        Task<bool> Message(UserInfo userinfo, Message msg);
        Task<MessageListResult> ReadHistory();
        Task<MembersResult> GetMembers();
        Task ClearMembers();
    }
}
