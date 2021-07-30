using System.Threading.Tasks;

namespace FunChat.GrainIntefaces
{
    public interface IHello : Orleans.IGrainWithGuidKey
    {
        Task<string> SayHello(string greeting);
    }
}