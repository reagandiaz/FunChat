namespace FunChat.GrainIntefaces
{
    public class CurrentChannelsResult
    {
        public ResultState State { get; set; } = ResultState.Failed;
        public string[] Items { get; set; }
    }
}
