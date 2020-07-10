using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml.XPath;
using HtmlAgilityPack;
using Playnite.SDK;

namespace DLSiteMetadata
{
    public enum DLSiteAgeRating
    {
        /// <summary>
        /// All ages
        /// </summary>
        All,
        /// <summary>
        /// R-15
        /// </summary>
        Teen,
        /// <summary>
        /// Adults
        /// </summary>
        Adults
    }

    internal static partial class Utils
    {
        internal static DLSiteAgeRating ToAgeRating(string rating)
        {
            switch (rating)
            {
                case "X-rated":
                    return DLSiteAgeRating.Adults;
                case "R-15":
                    return DLSiteAgeRating.Teen;
                default:
                    return DLSiteAgeRating.All;
            }
        }
    }

    public class DLSiteGame
    {
        public string DLSiteLink { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Circle { get; private set; }
        public string CircleLink { get; private set; }

        public List<string> ImageURLs { get; private set; }
        public double Rating { get; private set; }

        public string Release { get; private set; }
        public string LastModified { get; private set; }
        public DLSiteAgeRating AgeRating { get; private set; }

        public List<string> WorkFormats { get; private set; }
        public string FileFormat { get; private set; }

        public List<string> Genres { get; private set; }
        public string FileSize { get; private set; }

        public static async Task<DLSiteGame> LoadGame(string id, ILogger logger)
        {
            var url = Consts.GetWorkURL(id, id.StartsWith("RE"));

            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(url);
            if (document == null)
                return null;

            var node = document.DocumentNode;

            //logger.Info(node.InnerHtml);

            var game = new DLSiteGame {DLSiteLink = url};

            var nameNode = node.SelectSingleNode("//div[@id='top_wrapper']/div[@class='base_title_br clearfix']/h1[@id='work_name']/a");
            if(nameNode == null)
                logger.Warn($"Found no name node for {id}");
            else
            {
                var sName = nameNode.DecodeInnerText();
                if (sName.IsEmpty())
                {
                    logger.Warn($"Name for {id} is empty!");
                }
                else
                {
                    game.Name = sName;
                }
            }

            var imageNodes =
                node.SelectNodes(
                    "//div[@id='work_header']/div[@id='work_left']/div/div[@class='product-slider']/div[@class='product-slider-data']/div");
            if (imageNodes == null || imageNodes.Count == 0)
                logger.Warn($"Found no images for {id}!");
            else
            {
                game.ImageURLs = imageNodes.Select(x =>
                {
                    var src = x.GetValue("data-src");
                    if (src.StartsWith("//"))
                        src = $"https:{src}";
                    return src;
                }).NotNull().ToList();
            }

            //TODO: ratings
            //ratings require script execution

            /*var ratingsNode =
                node.SelectSingleNode(
                    "//div[@id='work_right']/div[@class='work_right_info']/div[@class='work_right_info_item'][2]/dl[@class='work_right_info_title']//span[@class='point average_count']");
            if(ratingsNode == null)
                logger.Warn($"Found no ratings node for {id}!");
            else
            {
                var sRating = ratingsNode.DecodeInnerText();
                if(sRating.IsEmpty())
                    logger.Warn($"Rating for {id} is empty!");
                else
                {
                    if(!double.TryParse(sRating, out var rating))
                        logger.Warn($"Unable to parse rating {sRating} to double!");
                    else
                        game.Rating = rating;
                }
            }*/

            var workRightNode = node.SelectSingleNode("//div[@id='work_right']/div[@id='work_right_inner']");
            if (workRightNode == null)
            {
                logger.Warn($"Found no work right div for {id}!");
                return game;
            }

            var circleNode = workRightNode.SelectSingleNode("//div[@id='work_right_name']/table[@id='work_maker']/tr/td/span[@class='maker_name']/a");
            if (circleNode == null)
                logger.Warn($"Found no circle node for {id}");
            else
            {
                var sCircle = circleNode.DecodeInnerText();
                if (sCircle.IsEmpty())
                    logger.Warn($"Circle for {id} is empty!");
                else
                    game.Circle = sCircle;

                var sCircleLink = circleNode.GetValue("href");
                if (sCircleLink.IsEmpty())
                    logger.Warn($"Circle link for {id} is empty!");
                else
                    game.CircleLink = sCircleLink;
            }

            var descriptionNode = node.SelectSingleNode("//div[@class='work_parts_container']/div[@class='work_parts type_text']/div[@class='work_parts_area']/p");
            if (descriptionNode == null)
                logger.Warn($"Found no description node for {id}");
            else
            {
                var sDescription = descriptionNode.InnerHtml;
                if (sDescription.IsEmpty())
                {
                    logger.Warn($"Description for {id} is empty!");
                }
                else
                {
                    game.Description = HttpUtility.HtmlDecode(sDescription);
                }
            }

            var tableChildren = workRightNode.SelectNodes("//table[@id='work_outline']//tr");
            if (tableChildren == null || tableChildren.Count == 0)
            {
                logger.Warn($"Table is null or has no children for {id}!");
                return game;
            }

            Dictionary<string, HtmlNode> dic = tableChildren.ToDictionary(x =>
            {
                var th = x.SelectSingleNode("th");
                return th.DecodeInnerText();
            }, x => x.SelectSingleNode("td"));

            dic.Do(x =>
            {
                var key = x.Key;
                var td = x.Value;

                switch (key)
                {
                    case "Release":
                    {
                        var dateNode = td.SelectSingleNode("a");
                        if(dateNode == null)
                            logger.Warn($"Table has release key but no a node for {id}!");
                        else
                        {
                            var sDate = dateNode.DecodeInnerText();
                            if (sDate.IsEmpty())
                                logger.Warn($"Date for {id} is empty!");
                            else
                                game.Release = sDate;
                        }

                        break;
                    }
                    case "Last Modified":
                    {
                        var sLastModified = td.DecodeInnerText();
                        if (sLastModified.IsEmpty())
                            logger.Warn($"Last Modified for {id} is empty!");
                        else
                            game.LastModified = sLastModified;
                        break;
                    }
                    case "Age Ratings":
                    {
                        var ratingNode = td.SelectSingleNode("div[@class='work_genre']/a/span");
                        if(ratingNode == null)
                            logger.Warn($"Found no rating node for {id}!");
                        else
                        {
                            var sRating = ratingNode.DecodeInnerText();
                            if (sRating.IsEmpty())
                                logger.Warn($"Rating for {id} is empty!");
                            else
                                game.AgeRating = Utils.ToAgeRating(sRating);
                        }

                        break;
                    }
                    case "Work Format":
                    {
                        var formatNodes = td.SelectNodes("div[@class='work_genre']/a/span");
                        if(formatNodes == null || formatNodes.Count == 0)
                            logger.Warn($"Work Format is null or has no elements for {id}!");
                        else
                        {
                            game.WorkFormats = formatNodes.Select(y => y.DecodeInnerText()).NotNull().ToList();
                        }

                        break;
                    }
                    case "File Format":
                    {
                        var fileFormatNode = td.SelectSingleNode("div[@class='work_genre']/a/span");
                        if (fileFormatNode == null)
                            logger.Warn($"Found no file format node for {id}!");
                        else
                        {
                            var sFileFormat = fileFormatNode.DecodeInnerText();
                            if (sFileFormat.IsEmpty())
                                logger.Warn($"File Format for {id} is empty!");
                            else
                                game.FileFormat = sFileFormat;
                        }

                        break;
                    }
                    case "Genre":
                    {
                        var genreNodes = td.SelectNodes("div[@class='main_genre']/a");
                        if (genreNodes == null || genreNodes.Count == 0)
                            logger.Warn($"Genres is null or has no elements for {id}!");
                        else
                        {
                            game.Genres = genreNodes.Select(y => y.DecodeInnerText()).NotNull().ToList();
                        }

                        break;
                    }
                    case "File Size":
                    {
                        var fileSizeNode = td.SelectSingleNode("div[@class='main_genre']");
                        if (fileSizeNode == null)
                            logger.Warn($"Found no file size node for {id}!");
                        else
                        {
                            var sFileSize = fileSizeNode.DecodeInnerText();
                            if (sFileSize.IsEmpty())
                                logger.Warn($"File Size for {id} is empty!");
                            else
                                game.FileSize = sFileSize;
                        }

                        break;
                    }
                }
            });

            return game;
        }
    }
}
