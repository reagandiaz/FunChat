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
        const string match = "[A-Za-z0-9_]";
        public bool IsValid(int minlength, int maxlength)
        {
            return minlength <= name.Length && name.Length <= maxlength && (new Regex(match).IsMatch(name));
        }
    }
}
