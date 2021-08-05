namespace FunChat.GrainIntefaces
{
    public class ChannelInfoResult
    {
        public ResultState State { get; set; } = ResultState.Failed;
        public ChannelInfo Info { get; set; }
    }
}
