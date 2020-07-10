using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HtmlAgilityPack;

namespace DLSiteMetadata
{
    internal static partial class Utils
    {
        internal static bool IsEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        internal static void Do<T>(this IEnumerable<T> col, Action<T> a)
        {
            foreach(var item in col) a(item);
        }

        internal static IEnumerable<T> NotNull<T>(this IEnumerable<T> col)
        {
            return col.Where(x => x != null);
        }

        internal static string GetValue(this HtmlNode node, string attr)
        {
            if (!node.HasAttributes)
                return null;
            var value = node.GetAttributeValue(attr, string.Empty);

            return value.IsEmpty() ? null : value;
        }

        internal static string DecodeInnerText(this HtmlNode node)
        {
            return HttpUtility.HtmlDecode(node?.InnerText);
        }
    }
}
