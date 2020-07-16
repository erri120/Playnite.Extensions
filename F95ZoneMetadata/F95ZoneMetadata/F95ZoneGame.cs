using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Extensions.Common;
using HtmlAgilityPack;
using Playnite.SDK;

namespace F95ZoneMetadata
{
    public class F95ZoneGame
    {
        public string F95Link { get; private set; }
        public string Name { get; private set; }
        public string Overview { get; private set; }
        public string Developer { get; private set; }

        //public string Updated { get; set; }
        public List<string> LabelList { get; private set; }
        //public List<string> OSList { get; set; }
        //public List<Link> StoreLinks { get; set; }

        public string CoverImageURL { get; private set; }
        public List<string> PreviewImageURLs { get; private set; }

        public List<string> Genres { get; private set; }

        public static string Root => "https://f95zone.to/threads/";

        public static async Task<F95ZoneGame> LoadGame(string url, ILogger logger)
        {
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(url);
            if (document == null)
                return null;

            var game = new F95ZoneGame
            {
                F95Link = url
            };

            var node = document.DocumentNode;
            var bodyNode = node.SelectSingleNode("//div[@class='uix_contentWrapper']/div[@class='p-body-main  ']/div[@class='p-body-content']");
            if (bodyNode.IsNull(logger, "Body", url))
                return null;

            var headerNode =
                bodyNode.SelectSingleNode(
                    "//div[@class='pageContent']/div[@class='uix_headerInner']");
            if (headerNode.IsNull(logger, "Header", url))
                return null;

            var labels = headerNode.SelectNodes("div[@class='p-title ']/h1[@class='p-title-value']/a[@class='labelLink']");
            if (!labels.IsNullOrEmpty(logger, "Labels", url))
            {
                game.LabelList = labels.Select(x => 
                    !x.TryGetInnerText("span", logger, "Label", url, out var label) 
                        ? null 
                        : label)
                    .NotNull().ToList();
            }

            if (headerNode.TryGetInnerText(
                "div[@class='p-title ']/h1[@class='p-title-value']",
                logger, "Title", url, out var id))
            {
                if(game.LabelList == null)
                    game.Name = id;
                else
                {
                    game.LabelList = game.LabelList.Select(label =>
                    {
                        if (id.Contains(label))
                            id = id.Replace(label, "");

                        if (label.StartsWith("["))
                            label = label.Substring(1, label.Length - 1);

                        if (label.EndsWith("]"))
                            label = label.Substring(0, label.Length - 1);

                        return label;
                    }).ToList();

                    id = id.Trim();

                    var lastStartingBracket = id.LastIndexOf('[');
                    var lastClosingBracket = id.LastIndexOf(']');

                    if (lastStartingBracket != -1 && lastClosingBracket != -1)
                    {
                        var dev = id.Substring(lastStartingBracket+1, lastClosingBracket-lastStartingBracket-1);
                        game.Developer = dev;
                    }

                    id = id.Substring(0, lastStartingBracket).Trim();
                    game.Name = id;
                }
            }
            else
            {
                return null;
            }

            var tags = headerNode.SelectNodes(
                "div[@class='p-description']/ul/li[@class='groupedTags']/a[@class='tagItem']");
            if (!tags.IsNullOrEmpty(logger, "Tags", id))
            {
                game.Genres = tags.Select(x =>
                {
                    var innerText = x.DecodeInnerText();
                    return innerText.IsEmpty(logger, "Tag", id)
                        ? null
                        : innerText;
                    /*var ti = new CultureInfo("en-US").TextInfo;
                    if (innerText.IsEmpty(logger, "Tag", id))
                        return null;

                    if (innerText == "2dcg")
                        return "2DCG";
                    if (innerText == "3dcg")
                        return "3DCG";

                    return ti.ToTitleCase(innerText);*/
                }).NotNull().ToList();
            }

            var contentNode = bodyNode.SelectSingleNode("//div[@class='message-inner']/div[@class='message-cell message-cell--main']/div[@class='message-main uix_messageContent js-quickEditTarget']/div/div/article[@class='message-body js-selectToQuote']/div[@class='bbWrapper']");
            if (contentNode.IsNull(logger, "Content", id))
                return null;

            var topNode = contentNode.SelectSingleNode("div");
            if (!topNode.IsNull(logger, "Top", id))
            {
                var coverImageNode = topNode.SelectSingleNode("a");
                if (!coverImageNode.IsNull(logger, "Cover Image", id))
                {
                    var href = coverImageNode.GetValue("href");
                    if (!href.IsEmpty(logger, "Cover Image", id))
                        game.CoverImageURL = href;
                }

                topNode.RemoveChild(coverImageNode);
                game.Overview = HttpUtility.HtmlDecode(topNode.InnerHtml);
            }

            var previewImages = contentNode.SelectNodes("//img[@class='bbImage ']");
            if (!previewImages.IsNullOrEmpty(logger, "Preview Images", id))
            {
                game.PreviewImageURLs = previewImages.Select(x =>
                {
                    var a = x.ParentNode;
                    var href = a.GetValue("href");
                    return href.IsEmpty(logger, "Preview Image href", id) 
                        ? null 
                        : href;
                }).NotNull().ToList();
            }

            return game;
        }
    }
}
