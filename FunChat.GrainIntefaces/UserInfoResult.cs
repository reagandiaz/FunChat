namespace FunChat.GrainIntefaces
{
    public class UserInfoResult
    {
        public ResultState State { get; set; } = ResultState.Failed;
        public UserInfo Info { get; set; }
    }
}
