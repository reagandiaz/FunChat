using FunChat.GrainIntefaces;
using Orleans.Streams;
using System;
using System.Threading.Tasks;

namespace FunChat.Client
{
    public class ChannelObserver : IAsyncObserver<Message>
    {
        readonly string channelname;
        public ChannelObserver(string channelname)
        {
            this.channelname = channelname;
        }
        public Task OnCompletedAsync()
        {
            Console.WriteLine($"/observer {this.channelname} : Complete");
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            Console.WriteLine($"/observer {this.channelname} : Error : {ex.Message} : {ex.StackTrace}");
            return Task.CompletedTask;
        }

        public Task OnNextAsync(Message item, StreamSequenceToken token = null)
        {
            Console.WriteLine($"/observer from {item.Channel} : {item.Author} : {item.Text}");
            return Task.CompletedTask;
        }
    }
}