using System.Threading.Tasks;

namespace FunChat.GrainIntefaces
{
    public interface IAuthenticator 
    {
        Task<string> Login(string name, string password);
    }
}
