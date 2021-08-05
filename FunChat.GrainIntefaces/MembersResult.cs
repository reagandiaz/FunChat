namespace FunChat.GrainIntefaces
{
    public class MembersResult
    {
        public ResultState State { get; set; } = ResultState.Failed;
        public string[] Items { get; set; }
    }
}
