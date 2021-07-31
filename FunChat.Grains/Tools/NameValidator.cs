using System.Text.RegularExpressions;

namespace FunChat.Grains.Tools
{
    public class NameValidator
    {
        readonly string name;
        public NameValidator(string name)
        {
            this.name = name;
        }
        const string match = @"^[a-zA-Z0-9]+$";
        public bool IsValid(int minlength, int maxlength)
        {
            return minlength <= name.Length && name.Length <= maxlength && (new Regex(match).IsMatch(name));
        }
    }
}
