using System.Text.RegularExpressions;

namespace GameBuilderEditor
{
    internal sealed class StringUtils
    {
        private static readonly string s_pattern = @"\d+";

        public static string IncrementIntegerInString(string str, int increment)
        {
            var matches = Regex.Matches(str, s_pattern);
            if (matches.Count > 0)
            {
                var lastMatch = matches[^1].Value;
                var integer = int.Parse(lastMatch);
                integer += increment;
                int index = str.LastIndexOf(lastMatch);
                str = str.Remove(index, lastMatch.Length).Insert(index, integer.ToString());
            }
            return str;
        }
    }
}
