using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Extensions.Common;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Playnite.SDK;

namespace DLSiteMetadata
{
    public class DLSiteGenre
    {
        public readonly int ID;
        public string JPN { get; set; }
        public string ENG { get; set; }

        public DLSiteGenre(int id)
        {
            ID = id;
        }

        [JsonIgnore]
        public string GetENGLink => $"https://www.dlsite.com/ecchi-eng/fsr/=/genre/{ID}/from/work.genre";
        [JsonIgnore]
        public string GetJPNLink => $"https://www.dlsite.com/maniax/fsr/=/genre/{ID}/from/work.genre";

        private static int GetID(string url, string baseURL)
        {
            var s = url.Replace(baseURL, "");
            s = s.Replace("/from/work.genre", "");

            if (!int.TryParse(s, out var id))
                return -1;
            return id;
        }

        public static int GetENGID(string url)
        {
            return GetID(url, "https://www.dlsite.com/ecchi-eng/fsr/=/genre/");
        }

        public static int GetJPNID(string url)
        {
            return GetID(url, "https://www.dlsite.com/maniax/fsr/=/genre/");
        }

        private static string ENGNameSuffix => " product list";
        private static string JPNNameSuffix => "の作品一覧";

        public static string CleanName(string name, bool isEnglish)
        {
            if (isEnglish)
            {
                return !name.EndsWith(ENGNameSuffix, StringComparison.OrdinalIgnoreCase) 
                    ? name : 
                    name.Substring(0, name.Length - ENGNameSuffix.Length);
            }

            return !name.EndsWith(JPNNameSuffix, StringComparison.OrdinalIgnoreCase)
                ? name 
                : name.Substring(0, name.Length - JPNNameSuffix.Length);
        }

        public override string ToString()
        {
            var hasJPN = JPN != null;
            var hasENG = ENG != null;
            if (!hasENG && !hasJPN)
                return $"{ID}";

            return $"{ID}: {(hasJPN ? "JPN: " + JPN : "")} {(hasENG ? "ENG: " + ENG : "")}";
        }
    }

    public class DLSiteGenreComparer : EqualityComparer<DLSiteGenre>
    {
        public override bool Equals(DLSiteGenre x, DLSiteGenre y)
        {
            if (x == null || y == null)
                return false;

            return x.ID == y.ID;
        }

        public override int GetHashCode(DLSiteGenre obj)
        {
            return obj == null ? 0 : obj.ID;
        }
    }

    public static class DLSiteGenres
    {
        private static HashSet<DLSiteGenre> _genres;
        private static readonly object LockObject = new object();

        static DLSiteGenres()
        {
            _genres = new HashSet<DLSiteGenre>(new DLSiteGenreComparer());
        }

        public static bool TryGetGenre(int id, out DLSiteGenre genre)
        {
            genre = null;

            var first = _genres.FirstOrDefault(x => x.ID == id);
            if (first == null)
                return false;

            genre = first;
            return true;
        }

        public static int LoadGenres(string dataDir)
        {
            var file = Path.Combine(dataDir, "genres.json");
            if (!File.Exists(file))
                return 0;

            var content = File.ReadAllText(file, Encoding.UTF8);
            //might be a bit expensive
            _genres = new HashSet<DLSiteGenre>(content.FromJson<List<DLSiteGenre>>(), new DLSiteGenreComparer());
            return _genres.Count;
        }

        public static void AddGenres(IEnumerable<DLSiteGenre> col)
        {
            col.Do(a => _genres.Add(a));
        }

        public static void SaveGenres(string dataDir)
        {
            lock (LockObject)
            {
                var file = Path.Combine(dataDir, "genres.json");
                var list = new List<DLSiteGenre>(_genres);
                var content = list.ToJson();
                File.WriteAllText(file, content, Encoding.UTF8);
            }
        }

        public static string ConvertTo(DLSiteGenre genre, ILogger logger, bool english)
        {
            var url = english ? genre.GetJPNLink : genre.GetENGLink;

            var web = new HtmlWeb();
            var document = web.Load(url);

            if (document == null)
            {
                logger.Error($"Document for {url} is null!");
                return null;
            }

            var node = document.DocumentNode;
            var id = genre.ID.ToString();

            if (node.IsNull(logger, "Document Node", id))
                return null;

            var titleNode = node.SelectSingleNode("//div[@id='container']/div[@id='wrapper']/div[@id='main']/div[@id='main_inner']/div[@class='base_title_br clearfix']/h1/span[@class='original_name']");
            if (titleNode.IsNull(logger, "Title Node", id))
                return null;

            var title = titleNode.DecodeInnerText();
            if (string.IsNullOrEmpty(title))
                return null;

            return DLSiteGenre.CleanName(title, !english);
        }
    }
}
