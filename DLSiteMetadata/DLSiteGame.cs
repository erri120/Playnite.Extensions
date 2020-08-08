using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Extensions.Common;
using HtmlAgilityPack;
using Playnite.SDK;
using Playnite.SDK.Metadata;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace DLSiteMetadata
{
    public class DLSiteGame : AGame
    {
        public override string Name { get; set; }
        public override string Description { get; set; }
        public override string Link { get; set; }

        public Link Circle { get; set; }
        public List<string> ImageURLs { get; set; }
        public DateTime Release { get; set; }
        public List<DLSiteGenre> Genres { get; set; }

        public DLSiteGame() { }
        public DLSiteGame(ILogger logger, string id) : base(logger, id) { }

        public override async Task<AGame> LoadGame()
        {
            var id = ID;

            var isEnglish = id.StartsWith("RE");
            var url = Consts.GetWorkURL(id, isEnglish);

            Link = url;

            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(url);
            if (document == null)
                throw new Exception($"Could not load from {url}");

            var node = document.DocumentNode;

            #region Name

            if (TryGetInnerText(node,
                "//div[@id='top_wrapper']/div[@class='base_title_br clearfix']/h1[@id='work_name']/a", "Name",
                out var name))
            {
                Name = name;
                AvailableFields.Add(MetadataField.Name);
            }

            #endregion

            #region Background and Cover Image(s)

            var imageNodes = node.SelectNodes("//div[@id='work_header']/div[@id='work_left']/div/div[@class='product-slider']/div[@class='product-slider-data']/div");
            if (!IsNullOrEmpty(imageNodes, "Images"))
            {
                List<string> images = imageNodes.Select(x =>
                {
                    var src = x.GetValue("data-src");
                    if (src.StartsWith("//"))
                        src = $"https:{src}";
                    return src;
                }).NotNull().ToList();

                if (images.Count > 0)
                {
                    ImageURLs = images;
                    AvailableFields.Add(MetadataField.BackgroundImage);
                    AvailableFields.Add(MetadataField.CoverImage);
                }
            }

            #endregion

            #region Description

            var descriptionNode = node.SelectSingleNode("//div[@class='work_parts_container']/div[@class='work_parts type_text']/div[@class='work_parts_area']/p");
            if (!IsNull(descriptionNode, "Description"))
            {
                var sDescription = descriptionNode.InnerHtml;
                if (!IsEmpty(sDescription, "Description"))
                {
                    Description = HttpUtility.HtmlDecode(sDescription);
                    AvailableFields.Add(MetadataField.Description);
                }
            }

            #endregion

            var workRightNode = node.SelectSingleNode("//div[@id='work_right']/div[@id='work_right_inner']");
            if (IsNull(workRightNode, "Work Right Div"))
                return this;

            #region Circle (Developer/Publisher)

            var circleNode = workRightNode.SelectSingleNode("//div[@id='work_right_name']/table[@id='work_maker']/tr/td/span[@class='maker_name']/a");
            if (!IsNull(circleNode, "Circle"))
            {
                var sCircle = circleNode.DecodeInnerText();
                if (!IsEmpty(sCircle, "Circle"))
                {
                    var sCircleLink = circleNode.GetValue("href");
                    if (!IsEmpty(sCircleLink, "Circle Link"))
                    {
                        Circle = new Link(sCircle, sCircleLink);
                        AvailableFields.Add(MetadataField.Developers);
                        AvailableFields.Add(MetadataField.Publishers);
                    }
                }
            }

            #endregion


            #region Extra Info in table

            var tableChildren = workRightNode.SelectNodes("//table[@id='work_outline']//tr");
            if (IsNullOrEmpty(tableChildren, "Table"))
                return this;

            Dictionary<string, HtmlNode> dic = tableChildren.ToDictionary(x =>
            {
                var th = x.SelectSingleNode("th");
                return th.DecodeInnerText();
            }, x => x.SelectSingleNode("td"));

            dic.Do(x =>
            {
                var key = x.Key;
                var td = x.Value;

                #region Release Date

                if (key == Consts.GetReleaseTranslation(isEnglish))
                {
                    var dateNode = td.SelectSingleNode("a");
                    if (IsNull(dateNode, "Release Date")) return;

                    var sDate = dateNode.DecodeInnerText();
                    if (IsEmpty(sDate, "Release Date")) return;

                    if (!DateTime.TryParse(sDate, out var release))
                        return;

                    Release = release;
                    AvailableFields.Add(MetadataField.ReleaseDate);

                    return;
                }

                #endregion

                #region Genres

                if (key == Consts.GetGenreTranslation(isEnglish))
                {
                    var genreNodes = td.SelectNodes("div[@class='main_genre']/a");
                    if (!IsNullOrEmpty(genreNodes, "Genres"))
                    {
                        List<DLSiteGenre> genres = genreNodes.Select(genreNode =>
                        {
                            var genreUrl = genreNode.GetValue("href");
                            var genreID = isEnglish ? DLSiteGenre.GetENGID(genreUrl) : DLSiteGenre.GetJPNID(genreUrl);
                            if (genreID == -1)
                            {
                                Logger.Error($"Could not get ID from {genreUrl}");
                                return null;
                            }

                            if (DLSiteGenres.TryGetGenre(genreID, out var cachedGenre))
                            {
                                if (cachedGenre.ENG != null)
                                    return cachedGenre;
                            }

                            var genre = new DLSiteGenre(genreID);
                            var genreName = genreNode.DecodeInnerText();
                            if (isEnglish)
                            {
                                genre.ENG = genreName;
                            }
                            else
                            {
                                genre.JPN = genreName;
                                var resultConvert = DLSiteGenres.ConvertTo(genre, Logger, isEnglish);
                                if (string.IsNullOrEmpty(resultConvert))
                                {
                                    Logger.Error($"Unable to convert {genreName} to English genre!");
                                    return null;
                                }

                                genre.ENG = resultConvert;
                            }

                            return genre;
                        }).NotNull().ToList();

                        if (genres.Count <= 0) return;

                        Genres = genres;
                        AvailableFields.Add(MetadataField.Genres);
                    }

                    return;
                }

                #endregion

                #region Not Usables

                //Last Modified (not useable)
                /*if (key == Consts.GetLastModifiedTranslation(isEnglish))
                {
                    var sLastModified = td.DecodeInnerText();
                    if (!IsEmpty(sLastModified, "Last Modified"))
                    {
                        game.LastModified = sLastModified;
                    }
                    return;
                }*/

                //Age Rating (not useable)
                /*if (key == Consts.GetAgeRatingsTranslation(isEnglish))
                {
                    var ratingNode = td.SelectSingleNode("div[@class='work_genre']/a/span");
                    if (!IsNull(ratingNode, "Age Rating"))
                    {
                        var sRating = ratingNode.DecodeInnerText();
                        if (!IsEmpty(sRating, "Age Rating"))
                            game.AgeRating = Utils.ToAgeRating(sRating);
                    }

                    return;
                }*/

                //Work Format (not useable)
                /*if (key == Consts.GetWorkFormatTranslation(isEnglish))
                {
                    var formatNodes = td.SelectNodes("div[@class='work_genre']/a/span");
                    if (!IsNullOrEmpty(formatNodes, "Work Format"))
                        game.WorkFormats = formatNodes.Select(y => y.DecodeInnerText()).NotNull().ToList();

                    return;
                }*/

                //File Format (not useable)
                /*if (key == Consts.GetFileFormatTranslation(isEnglish))
                {
                    var fileFormatNode = td.SelectSingleNode("div[@class='work_genre']/a/span");
                    if (!IsNull(fileFormatNode, "File Format"))
                    {
                        var sFileFormat = fileFormatNode.DecodeInnerText();
                        if (!IsEmpty(sFileFormat, "File Format"))
                            game.FileFormat = sFileFormat;
                    }

                    return;
                }*/

                //File Size (not useable)
                /*if (key == Consts.GetFileSizeTranslation(isEnglish))
                {
                    var fileSizeNode = td.SelectSingleNode("div[@class='main_genre']");
                    if (!IsNull(fileSizeNode, "File Size"))
                    {
                        var sFileSize = fileSizeNode.DecodeInnerText();
                        if (!IsEmpty(sFileSize, "File Size"))
                            game.FileSize = sFileSize;
                    }

                    return;
                }*/

                #endregion

                Logger.Warn($"Unknown key: {key}");
            });

            #endregion

            return this;
        }

        private MetadataFile GetImage(bool coverImage, IDialogsFactory dialogsAPI)
        {
            List<ImageFileOption> options = ImageURLs
                .Select(x => new ImageFileOption(x))
                .ToList();

            var option = dialogsAPI.ChooseImageFile(options, coverImage ? "Select Cover Image" : "Select Background Image");
            if (option == null)
                return null;

            var file = new MetadataFile(option.Path);
            return file;
        }

        public override MetadataFile GetCoverImage(IDialogsFactory dialogsAPI)
        {
            return GetImage(true, dialogsAPI);
        }

        public override MetadataFile GetBackgroundImage(IDialogsFactory dialogsAPI)
        {
            return GetImage(false, dialogsAPI);
        }

        public override DateTime GetReleaseDate()
        {
            return Release;
        }

        public override List<string> GetGenres()
        {
            return Genres.Select(x => x.ENG).ToList();
        }

        public override List<Link> GetLinks()
        {
            return new List<Link>
            {
                new Link("DLSite", Link),
                Circle
            };
        }

        public override List<string> GetDevelopers()
        {
            return new List<string> { Circle.Name };
        }

        public override List<string> GetPublishers()
        {
            return GetDevelopers();
        }

        #region Not Implemented

        public override int GetCommunityScore()
        {
            throw new NotImplementedException();
        }

        public override int GetCriticScore()
        {
            throw new NotImplementedException();
        }

        public override List<string> GetFeatures()
        {
            throw new NotImplementedException();
        }

        public override MetadataFile GetIcon()
        {
            throw new NotImplementedException();
        }

        public override List<string> GetTags()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
