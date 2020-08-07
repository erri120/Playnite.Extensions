using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace Extensions.Common
{
    public static class Utils
    {
        #region JSON Stuff

        private static JsonSerializerSettings GenericJsonSettings => new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        public static string ToJson<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj, GenericJsonSettings);
        }

        public static T FromJson<T>(this string data)
        {
            return JsonConvert.DeserializeObject<T>(data, GenericJsonSettings);
        }

        #endregion

        public static bool TryGetLink(this Game game, string name, out Link link)
        {
            link = game.Links.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return link != null;
        }

        public static bool IsEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        public static bool IsEmpty(this string s, ILogger logger, string name, string id)
        {
            if (!IsEmpty(s))
                return false;

            logger.Warn($"{name} for {id} is empty!");
            return true;
        }

        public static bool IsNull(this HtmlNode node, ILogger logger, string name, string id)
        {
            if (node != null)
                return false;

            logger.Warn($"Found no {name} node for {id}!");
            return true;
        }

        public static bool IsNullOrEmpty(this HtmlNodeCollection collection, ILogger logger, string name, string id)
        {
            if (collection != null && collection.Count > 0)
                return false;

            logger.Warn($"Found no {name} for {id}!");
            return true;
        }

        public static bool TryGetInnerText(this HtmlNode baseNode, string xpath, ILogger logger, string name, string id, out string innerText)
        {
            innerText = null;
            var node = baseNode.SelectSingleNode(xpath);
            if (node.IsNull(logger, name, id))
                return false;

            var sText = node.DecodeInnerText();
            if (sText.IsEmpty(logger, name, id))
                return false;

            innerText = sText;
            return true;
        }

        public static void Do<T>(this IEnumerable<T> col, Action<T> a)
        {
            foreach (var item in col) a(item);
        }

        public static IEnumerable<T> NotNull<T>(this IEnumerable<T> col)
        {
            return col.Where(x => x != null);
        }

        public static string GetValue(this HtmlNode node, string attr)
        {
            if (!node.HasAttributes)
                return null;
            var value = node.GetAttributeValue(attr, string.Empty);

            return value.IsEmpty() ? null : value;
        }

        public static string DecodeInnerText(this HtmlNode node)
        {
            return HttpUtility.HtmlDecode(node?.InnerText);
        }
    }
}
