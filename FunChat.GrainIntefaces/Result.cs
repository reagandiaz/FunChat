namespace FunChat.GrainIntefaces
{
    public class Result<T>
    {
        public bool Success { get; set; }
        public T Info { get; set; }
    }
}
