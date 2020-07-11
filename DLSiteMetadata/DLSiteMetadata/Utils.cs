using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HtmlAgilityPack;
using Playnite.SDK;

namespace DLSiteMetadata
{
    internal static partial class Utils
    {
        internal static bool IsEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        internal static bool IsEmpty(this string s, ILogger logger, string name, string id)
        {
            if (!IsEmpty(s))
                return false;

            logger.Warn($"{name} for {id} is empty!");
            return true;
        }

        internal static bool IsNull(this HtmlNode node, ILogger logger, string name, string id)
        {
            if (node != null)
                return false;

            logger.Warn($"Found no {name} node for {id}!");
            return true;
        }

        internal static bool IsNullOrEmpty(this HtmlNodeCollection collection, ILogger logger, string name, string id)
        {
            if (collection != null && collection.Count > 0)
                return false;

            logger.Warn($"Found no {name} for {id}!");
            return true;
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
