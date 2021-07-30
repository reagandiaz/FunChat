using FunChat.GrainIntefaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FunChat.Grains
{
    public class HelloGrain : Orleans.Grain, IHello
    {
        private readonly ILogger logger;
        public HelloGrain(ILogger<HelloGrain> logger)
        {
            this.logger = logger;
        }
        Task<string> IHello.SayHello(string greeting)
        {
            logger.LogInformation($"\n SayHello message received: greeting = '{greeting}'");
            return Task.FromResult($"Client said: '{greeting}', so HelloGrain says: Hello!");
        }
    }
}