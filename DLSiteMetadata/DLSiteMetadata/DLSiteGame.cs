using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Extensions.Common;
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

    internal static class Utils
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
            var isEnglish = id.StartsWith("RE");
            var url = Consts.GetWorkURL(id, isEnglish);

            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(url);
            if (document == null)
                return null;

            var node = document.DocumentNode;

            //logger.Info(node.InnerHtml);

            var game = new DLSiteGame {DLSiteLink = url};

            var nameNode = node.SelectSingleNode("//div[@id='top_wrapper']/div[@class='base_title_br clearfix']/h1[@id='work_name']/a");
            if (!nameNode.IsNull(logger, "Name", id))
            {
                var sName = nameNode.DecodeInnerText();
                if (!sName.IsEmpty(logger, "Name", id))
                    game.Name = sName;
            }

            var imageNodes =
                node.SelectNodes(
                    "//div[@id='work_header']/div[@id='work_left']/div/div[@class='product-slider']/div[@class='product-slider-data']/div");
            if (!imageNodes.IsNullOrEmpty(logger, "Images", id))
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
            if (workRightNode.IsNull(logger, "Work Right Div", id))
                return game;

            var circleNode = workRightNode.SelectSingleNode("//div[@id='work_right_name']/table[@id='work_maker']/tr/td/span[@class='maker_name']/a");
            if (!circleNode.IsNull(logger, "Circle", id))
            {
                var sCircle = circleNode.DecodeInnerText();
                if (!sCircle.IsEmpty(logger, "Circle", id))
                    game.Circle = sCircle;

                var sCircleLink = circleNode.GetValue("href");
                if(!sCircleLink.IsEmpty(logger, "Circle Link", id))
                    game.CircleLink = sCircleLink;
            }

            var descriptionNode = node.SelectSingleNode("//div[@class='work_parts_container']/div[@class='work_parts type_text']/div[@class='work_parts_area']/p");
            if (!descriptionNode.IsNull(logger, "Description", id))
            {
                var sDescription = descriptionNode.InnerHtml;
                if (!sDescription.IsEmpty(logger, "Description", id))
                    game.Description = HttpUtility.HtmlDecode(sDescription);
            }

            var tableChildren = workRightNode.SelectNodes("//table[@id='work_outline']//tr");
            if(tableChildren.IsNullOrEmpty(logger, "Table", id))
                return game;

            Dictionary<string, HtmlNode> dic = tableChildren.ToDictionary(x =>
            {
                var th = x.SelectSingleNode("th");
                return th.DecodeInnerText();
            }, x => x.SelectSingleNode("td"));

            dic.Do(x =>
            {
                var key = x.Key;
                var td = x.Value;

                if (key == Consts.GetReleaseTranslation(isEnglish))
                {
                    var dateNode = td.SelectSingleNode("a");
                    if (!dateNode.IsNull(logger, "Release Date", id))
                    {
                        var sDate = dateNode.DecodeInnerText();
                        if (!sDate.IsEmpty(logger, "Release Date", id))
                            game.Release = sDate;
                    }

                    return;
                }
                
                if (key == Consts.GetLastModifiedTranslation(isEnglish))
                {
                    var sLastModified = td.DecodeInnerText();
                    if (!sLastModified.IsEmpty(logger, "Last Modified", id))
                        game.LastModified = sLastModified;
                    return;
                }

                if (key == Consts.GetAgeRatingsTranslation(isEnglish))
                {
                    var ratingNode = td.SelectSingleNode("div[@class='work_genre']/a/span");
                    if (!ratingNode.IsNull(logger, "Age Rating", id))
                    {
                        var sRating = ratingNode.DecodeInnerText();
                        if (!sRating.IsEmpty(logger, "Age Rating", id))
                            game.AgeRating = Utils.ToAgeRating(sRating);
                    }

                    return;
                }

                if (key == Consts.GetWorkFormatTranslation(isEnglish))
                {
                    var formatNodes = td.SelectNodes("div[@class='work_genre']/a/span");
                    if (!formatNodes.IsNullOrEmpty(logger, "Work Format", id))
                        game.WorkFormats = formatNodes.Select(y => y.DecodeInnerText()).NotNull().ToList();

                    return;
                }

                if (key == Consts.GetFileFormatTranslation(isEnglish))
                {
                    var fileFormatNode = td.SelectSingleNode("div[@class='work_genre']/a/span");
                    if (!fileFormatNode.IsNull(logger, "File Format", id))
                    {
                        var sFileFormat = fileFormatNode.DecodeInnerText();
                        if (!sFileFormat.IsEmpty(logger, "File Format", id))
                            game.FileFormat = sFileFormat;
                    }

                    return;
                }

                if (key == Consts.GetGenreTranslation(isEnglish))
                {
                    var genreNodes = td.SelectNodes("div[@class='main_genre']/a");
                    if (!genreNodes.IsNullOrEmpty(logger, "Genres", id))
                        game.Genres = genreNodes.Select(y => y.DecodeInnerText()).NotNull().ToList();

                    return;
                }

                if (key == Consts.GetFileSizeTranslation(isEnglish))
                {
                    var fileSizeNode = td.SelectSingleNode("div[@class='main_genre']");
                    if (!fileSizeNode.IsNull(logger, "File Size", id))
                    {
                        var sFileSize = fileSizeNode.DecodeInnerText();
                        if (!sFileSize.IsEmpty(logger, "File Size", id))
                            game.FileSize = sFileSize;
                    }

                    return;
                }

                logger.Warn($"Unknown key: {key}");
            });

            return game;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
