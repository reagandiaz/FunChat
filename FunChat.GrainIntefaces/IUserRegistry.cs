using System;
using System.Threading.Tasks;

namespace FunChat.GrainIntefaces
{
    public interface IUserRegistry: Orleans.IGrainWithGuidKey
    {
        Task<Guid> Add(string user, Guid key);
        Task Remove(string user);
        Task<Guid> Get(string user);
        Task<UserInfo[]> GetMany(string[] users);
    }
}
