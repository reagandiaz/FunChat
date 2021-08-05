namespace FunChat.GrainIntefaces
{
    public class MessageListResult
    {
        public ResultState State { get; set; } = ResultState.Failed;
        public Message[] Messages { get; set; }
    }
}
