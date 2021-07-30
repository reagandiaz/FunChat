using FunChat.GrainIntefaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FunChat.Grains
{
    public class UserRegistry : Orleans.Grain, IUserRegistry
    {
        readonly Dictionary<string, Guid> activeusers = new Dictionary<string, Guid>();
        public async Task<Guid> Add(string user, Guid key)
        {
            Guid guid;

            if (activeusers.ContainsKey(user))
                activeusers.TryGetValue(user, out guid);
            else
            {
                activeusers.Add(user, key);
                guid = key;
            }
            return await Task.FromResult(guid);
        }

        public async Task Remove(string user)
        {
            await Task.FromResult(activeusers.Remove(user));
        }

        public async Task<Guid> Get(string user)
        {
            Guid guid = Guid.Empty;

            if (activeusers.ContainsKey(user))
                activeusers.TryGetValue(user, out guid);

            return await Task.FromResult(guid);
        }

        public async Task<UserInfo[]> GetMany(string[] users)
        {
            return await Task.FromResult(activeusers.Where(s => users.Contains(s.Key)).Select(s => new UserInfo() { Key = s.Value, Name = s.Key }).ToArray());
        }
    }
}