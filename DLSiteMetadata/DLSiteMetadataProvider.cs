using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Extensions.Common;
using HtmlAgilityPack;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace DLSiteMetadata
{
    public class DLSiteMetadataProvider : AMetadataProvider<DLSiteGame>
    {
        protected override string GetID()
        {
            var playniteGame = Options.GameData;

            var name = playniteGame.Name;

            if (name.IsEmpty())
            {
                if (!playniteGame.TryGetLink("DLSite", out var dlSiteLink))
                    throw new Exception("Name must not be empty!");
                name = dlSiteLink.Url;
            }

            var id = name;

            IDCheck:
            if (id.StartsWith("RJ", StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("RE", StringComparison.OrdinalIgnoreCase)) return id;

            if (!id.StartsWith(Consts.RootENG) && !id.StartsWith(Consts.RootJPN))
                throw new Exception($"{id} does not start with RJ/RE!");

            //https://www.dlsite.com/ecchi-eng/work/=/product_id/RE234198.html
            var root = id.StartsWith(Consts.RootENG) ? Consts.RootENG : Consts.RootJPN;
            id = id.Replace(root, "");
            //work/=/product_id/{id}.html
            id = id.Replace(".html", "").Replace("work/=/product_id/", "");
            goto IDCheck;
        }

        public override async Task<DLSiteGame> LoadGame()
        {
            var id = ID;

            var isEnglish = id.StartsWith("RE");
            var url = Consts.GetWorkURL(id, isEnglish);

            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(url);
            if (document == null)
                throw new Exception($"Could not load from {url}");

            var node = document.DocumentNode;
            var game = new DLSiteGame {Link = url};

            if (Plugin?.PlayniteApi != null)
                game.PlayniteAPI = Plugin.PlayniteApi;

            #region Name

            if (TryGetInnerText(node,
                "//div[@id='top_wrapper']/div[@class='base_title_br clearfix']/h1[@id='work_name']/a", "Name",
                out var name))
            {
                game.Name = name;
                game.AvailableFields.Add(MetadataField.Name);
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
                    game.ImageURLs = images;
                    game.AvailableFields.Add(MetadataField.BackgroundImage);
                    game.AvailableFields.Add(MetadataField.CoverImage);
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
                    game.Description = HttpUtility.HtmlDecode(sDescription);
                    game.AvailableFields.Add(MetadataField.Description);
                }
            }

            #endregion

            var workRightNode = node.SelectSingleNode("//div[@id='work_right']/div[@id='work_right_inner']");
            if (IsNull(workRightNode, "Work Right Div"))
                return game;

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
                        game.Circle = new Link(sCircle, sCircleLink);
                        game.AvailableFields.Add(MetadataField.Developers);
                        game.AvailableFields.Add(MetadataField.Publishers);
                    }
                }
            }

            #endregion


            #region Extra Info in table

            var tableChildren = workRightNode.SelectNodes("//table[@id='work_outline']//tr");
            if (IsNullOrEmpty(tableChildren, "Table"))
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

                #region Release Date

                if (key == Consts.GetReleaseTranslation(isEnglish))
                {
                    var dateNode = td.SelectSingleNode("a");
                    if (IsNull(dateNode, "Release Date")) return;

                    var sDate = dateNode.DecodeInnerText();
                    if (IsEmpty(sDate, "Release Date")) return;

                    if (!DateTime.TryParse(sDate, out var release))
                        return;

                    game.Release = release;
                    game.AvailableFields.Add(MetadataField.ReleaseDate);

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

                        game.Genres = genres;
                        game.AvailableFields.Add(MetadataField.Genres);
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


            return game;
        }
    }
}