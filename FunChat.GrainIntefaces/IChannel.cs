using System;
using System.Threading.Tasks;

namespace FunChat.GrainIntefaces
{
    public interface IChannel : Orleans.IGrainWithGuidKey
    {
        Task Initialize(ChannelInfo channelinfo, string password);
        Task<string> Name();
        Task<Result<ChannelInfo>> Join(UserInfo userInfo, string password);
        Task<Result<ChannelInfo>> Leave(string username);
        Task<Result<ChannelInfo>> UpdateUserInfo(UserInfo userInfo);
        Task<bool> Message(UserInfo userinfo, Message msg);
        Task<Result<Message[]>> ReadHistory();
        Task<Result<string[]>> GetMembers();
        Task ClearMembers();
    }
}
