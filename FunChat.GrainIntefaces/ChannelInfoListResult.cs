namespace FunChat.GrainIntefaces
{
    public class ChannelInfoListResult
    {
        public ResultState State { get; set; } = ResultState.Failed;
        public ChannelInfo[] Infos { get; set; }
    }
}