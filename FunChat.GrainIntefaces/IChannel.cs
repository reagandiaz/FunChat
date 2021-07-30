using System;
using System.Threading.Tasks;

namespace FunChat.GrainIntefaces
{
    public interface IChannel : Orleans.IGrainWithGuidKey
    {
        Task SetChannelInfo(ChannelInfo channelinfo, string password);
        Task<string> Name();
        Task<ChannelInfo> Join(string username, string password);
        Task<ChannelInfo> Leave(string username);
        Task<bool> Message(Message msg);
        Task<string[]> GetMembers();
        Task ClearMembers();
        Task<Message[]> ReadHistory(int numberOfMessages);
    }
}
