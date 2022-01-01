using System;
using System.Linq;
using System.Text;

namespace Extensions.Common;

public static class Utils
{
    public static string CustomTrim(this string input)
    {
        // the problem: "Hello\n                   World" can not be trimmed using normal .Trim() function
        // using .Split(' ') and the StringSplitOptions.RemoveEmptyEntries options we can completely remove all whitespaces

        var sb = new StringBuilder(input);
        sb.Replace('\n', ' ');
        sb.Replace('\t', ' ');
        // NBSP
        sb.Replace('\u00A0', ' ');

        var res = sb.ToString()
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Aggregate((x, y) => $"{x} {y}");
        return res;
    }
}
