using FunChat.GrainIntefaces;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FunChat.UnitTest.Tools
{
    public class MessageObserver : IAsyncObserver<Message>
    {
        readonly List<Message> messages = new List<Message>();

        public List<Message> GetMessages()
        {
            return messages;
        }

        public Task OnCompletedAsync()
        {
            throw new NotImplementedException();
        }

        public Task OnErrorAsync(Exception ex)
        {
            throw new NotImplementedException();
        }

        public async Task OnNextAsync(Message item, StreamSequenceToken token = null)
        {
            messages.Add(item);
            await Task.CompletedTask;
        }
    }
}
