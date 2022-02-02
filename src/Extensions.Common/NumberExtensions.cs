using System;
using System.Globalization;

namespace Extensions.Common;

public static class NumberExtensions
{
    private static readonly string[] Suffix = {"B", "KB", "MB", "GB", "TB", "PB", "EB"}; // Longs run out around EB

    public static string ToFileSizeString(this long byteCount)
    {
        if (byteCount == 0)
            return "0" + Suffix[0];
        var bytes = Math.Abs(byteCount);
        var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        var num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return Math.Sign(byteCount) * num + Suffix[place];
    }

    public static bool TryParse(string? s, out double result)
    {
        result = double.NaN;
        return s is not null && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}
