using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Extensions.Common;
using HtmlAgilityPack;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace F95ZoneMetadata
{
    public class F95ZoneGame : AGame
    {
        public override string Name { get; set; }
        public override string Description { get; set; }
        public override string Link { get; set; }

        public MetadataNameProperty Developer { get; private set; }

        public List<MetadataNameProperty> Labels { get; private set; }
        public List<MetadataNameProperty> Genres { get; private set; }

        public List<Uri> Images { get; private set; }

        public ReleaseDate ReleaseDate { get; private set; }
        public double Rating { get; private set; } = -1.0;

        public F95ZoneGame() { }
        public F95ZoneGame(ILogger logger, string id) : base(logger, id) { }

        public override async Task<AGame> LoadGame()
        {
            Link = $"https://f95zone.to/threads/{ID}";

            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(Link);
            if (document == null)
                throw new Exception($"Could not load from {Link}");

            var node = document.DocumentNode;

            var headerNode = node.SelectSingleNode("//div[@class='pageContent']/div[@class='uix_headerInner']");
            if (IsNull(headerNode, "Header"))
                return null;

            var titleNode = headerNode.SelectSingleNode("div[@class='p-title ']/h1[@class='p-title-value']");
            if (IsNull(titleNode, "Title"))
                return null;

            #region Labels

            var labels = titleNode.SelectNodes("a[@class='labelLink']");
            if (!IsNullOrEmpty(labels, "Labels"))
            {
                List<MetadataNameProperty> temp = labels.Select(x =>
                {
                    if (!TryGetInnerText(x, "span", "Label Text", out string labelText))
                        return null;

                    var start = 0;
                    var length = labelText.Length;

                    if (labelText[0] == '[')
                    {
                        start = 1;
                        length--;
                    }

                    if (labelText[labelText.Length - 1] == ']')
                        length--;

                    return new MetadataNameProperty(labelText.Substring(start, length));
                }).NotNull().ToList();

                if (temp.Count > 0)
                {
                    Labels = temp;
                    AvailableFields.Add(MetadataField.Tags);
                    LogFound("Labels", Labels);
                }
            }

            #endregion

            #region Name

            var name = titleNode.DecodeInnerText();
            if (!IsEmpty(name, "Name"))
            {
                if (Labels != null && Labels.Count > 0)
                {
                    var last = Labels.Last();
                    var i = name.IndexOf(last.Name, StringComparison.OrdinalIgnoreCase);
                    name = name.Substring(i + last.Name.Length + 1).TrimStart();
                }

                var lastStartingBracket = name.LastIndexOf('[');
                var lastClosingBracket = name.LastIndexOf(']');

                if (lastStartingBracket != -1 && lastClosingBracket != -1)
                {
                    Developer = new MetadataNameProperty(name.Substring(lastStartingBracket + 1, lastClosingBracket - lastStartingBracket - 1));
                    AvailableFields.Add(MetadataField.Developers);
                    AvailableFields.Add(MetadataField.Publishers);
                }

                Name = name.Substring(0, lastStartingBracket).Trim();
                AvailableFields.Add(MetadataField.Name);
                LogFound("Name", Name);
            }

            #endregion

            #region Genres

            var tags = headerNode.SelectNodes("div[@class='p-description']/ul[@class='listInline listInline--bullet']/li[@class='groupedTags']/a[@class='tagItem']");
            if (!IsNullOrEmpty(tags, "Tags"))
            {
                List<MetadataNameProperty> temp = tags.Select(x => new MetadataNameProperty(x.DecodeInnerText())).NotNull().ToList();

                if (temp.Count > 0)
                {
                    Genres = temp;
                    AvailableFields.Add(MetadataField.Genres);
                    LogFound("Genres", Genres);
                }
            }

            #endregion

            #region Release Date

            var dateNode =
                headerNode.SelectSingleNode(
                    "div[@class='p-description']/ul[@class='listInline listInline--bullet']/li[2]");
            if (!IsNull(dateNode, "Date"))
            {
                var timeNode = dateNode.SelectSingleNode("a[@class='u-concealed']/time");
                if (!IsNull(timeNode, "Date Time"))
                {
                    var sDateTime = timeNode.GetValue("datetime");
                    if (!sDateTime.IsEmpty())
                    {
                        if (DateTime.TryParse(sDateTime, out var dateTime))
                        {
                            ReleaseDate = new ReleaseDate(dateTime);
                            AvailableFields.Add(MetadataField.ReleaseDate);
                            LogFound("Release Date", sDateTime);
                        }
                    }
                }
            }

            #endregion

            #region Ratings

            var ratingNode =
                node.SelectSingleNode(
                    "//div[@class='pageContent']/div[@class='uix_headerInner--opposite']/div[@class='p-title-pageAction']/span[@class='ratingStarsRow ']/span[@class='ratingStars bratr-rating ']");
            if (!IsNull(ratingNode, "Rating"))
            {
                var sRating = ratingNode.GetValue("title");
                if (!sRating.IsEmpty())
                {
                    if (TryParseRating(sRating, out var rating))
                    {
                        Rating = rating;
                        AvailableFields.Add(MetadataField.CommunityScore);
                        LogFound("Ratings", sRating);
                    }
                    else
                    {
                        Logger.Debug($"Unable to parse rating \"{sRating}\"");
                    }
                }
                else
                {
                    Logger.Debug("Rating Node Value is empty!");
                }
            }

            #endregion

            #region Cover Image

            var coverImageNode = headerNode.ParentNode.ParentNode;
            if (!IsNull(coverImageNode, "Cover Image"))
            {
                var style = coverImageNode.GetValue("style").DecodeString();
                if (!style.IsEmpty())
                {
                    var lastStartingBracket = style.LastIndexOf('(');
                    var lastClosingBracket = style.LastIndexOf(')');

                    if (lastStartingBracket != -1 && lastClosingBracket != -1)
                    {
                        //+2, -2 because ('URL') -> URL
                        var sCoverImage = style.Substring(lastStartingBracket + 2, lastClosingBracket - lastStartingBracket - 2);
                        if (Uri.TryCreate(sCoverImage, UriKind.Absolute, out var coverImage))
                        {
                            if (Images == null)
                                Images = new List<Uri>();
                            Images.Add(coverImage);
                            AvailableFields.Add(MetadataField.CoverImage);
                            AvailableFields.Add(MetadataField.BackgroundImage);
                            LogFound("Cover Image", null);
                        }
                    }
                }
            }

            #endregion

            #region Body

            var bodyNode = node.SelectSingleNode("//div[@class='p-body-pageContent']/div[@class='block block--messages']/div[@class='block-container lbContainer']/div[@class='block-body js-replyNewMessageContainer']/article/div[@class='message-inner']/div[@class='message-cell message-cell--main']/div/div[@class='message-content js-messageContent']/div/article/div[@class='bbWrapper']");
            if (IsNull(bodyNode, "Body")) return this;
            {
                #region Images

                var imageNodes = bodyNode.SelectNodes(bodyNode.XPath+"//img[@class='bbImage ']");
                if (!IsNullOrEmpty(imageNodes, "Body - Images"))
                {
                    List<Uri> temp = imageNodes.Select(x =>
                    {
                        var parent = x.ParentNode;
                        if (!parent.Name.Equals("a", StringComparison.OrdinalIgnoreCase))
                        {
                            var src = x.GetValue("src");
                            if (src.IsEmpty())
                                return null;

                            return !Uri.TryCreate(src, UriKind.Absolute, out var uriResult)
                                ? null
                                : uriResult;
                        }
                        else
                        {
                            var href = parent.GetValue("href");
                            if (href.IsEmpty())
                                return null;

                            return !Uri.TryCreate(href, UriKind.Absolute, out var uriResult)
                                ? null
                                : uriResult;
                        }
                    }).NotNull().ToList();

                    if (temp.Count > 0)
                    {
                        if (Images == null)
                            Images = temp;
                        else
                            Images.AddRange(temp);
                        if (!AvailableFields.Contains(MetadataField.CoverImage))
                            AvailableFields.Add(MetadataField.CoverImage);
                        if (!AvailableFields.Contains(MetadataField.BackgroundImage))
                            AvailableFields.Add(MetadataField.BackgroundImage);
                        
                        LogFound("Images", Images);
                    }
                }

                #endregion

                #region Description

                var innerText = bodyNode.InnerText.DecodeString();

                var overviewList = new List<string>
                {
                    "Overview",
                    "About this game"
                };

                Tuple<int, string> selectedOverview = overviewList.Select(x =>
                {
                    var y = $"{x}:";
                    var index = innerText.IndexOf(y, StringComparison.OrdinalIgnoreCase);
                    if (index != -1) return Tuple.Create(index, y);
                    
                    y = x;
                    index = innerText.IndexOf(y, StringComparison.OrdinalIgnoreCase);
                    return Tuple.Create(index, y);
                }).FirstOrDefault(x => x.Item1 != -1);

                if (selectedOverview == null) return this;
                {
                    var description = innerText.Substring(selectedOverview.Item1+ selectedOverview.Item2.Length);
                    var linksInfoIndex = description.IndexOf("You must be registered to see the links", StringComparison.OrdinalIgnoreCase);
                    description = description.Substring(0, linksInfoIndex);

                    var infoList = new List<string>
                    {
                        "Thread Updated",
                        "Updated",
                        "Release Date",
                        "Developer / Publisher",
                        "Developer",
                        "Publisher",
                        "Censorship",
                        "Version",
                        "Operating System",
                        "Platform",
                        "Language",
                        "Alternative version",
                        "Sidestory",
                        "Same Series",
                        "Prequel",
                        "Sequel",
                        "Changelog",
                        "Genre",
                        "Cheat",
                        "OS",
                    };

                    infoList.Select(x => $"{x}:").Do(x =>
                    {
                        var index = description.IndexOf(x, StringComparison.OrdinalIgnoreCase);
                        if (index == -1)
                            return;

                        description = description.Substring(0, index);
                    });

                    description = description.Trim();
                    if (description.IsEmpty()) return this;
                    
                    Description = description;
                    AvailableFields.Add(MetadataField.Description);
                    LogFound("Description", null);
                }

                #endregion
            }

            #endregion

            return this;
        }

        public static bool TryParseRating(string sRating, out double rating)
        {
            if (sRating.EndsWith("star(s)"))
                sRating = sRating.Replace("star(s)", "").Trim();

            return double.TryParse(sRating, NumberStyles.Float, CultureInfo.InvariantCulture, out rating);
        }
        
        public override IEnumerable<MetadataNameProperty> GetGenres()
        {
            return Genres;
        }

        public override IEnumerable<MetadataNameProperty> GetTags()
        {
            return Labels;
        }

        public override MetadataFile GetCoverImage(IDialogsFactory dialogsAPI)
        {
            List<ImageFileOption> options = Images.Select(x => new ImageFileOption(x.AbsoluteUri)).ToList();
            var option = dialogsAPI.ChooseImageFile(options, "Select Cover Image");
            return option == null ? null : new MetadataFile(option.Path);
        }

        public override MetadataFile GetBackgroundImage(IDialogsFactory dialogsAPI)
        {
            List<ImageFileOption> options = Images.Select(x => new ImageFileOption(x.AbsoluteUri)).ToList();
            var option = dialogsAPI.ChooseImageFile(options, "Select Background Image");
            return option == null ? null : new MetadataFile(option.Path);
        }

        public override ReleaseDate GetReleaseDate()
        {
            return ReleaseDate;
        }

        public override List<Link> GetLinks()
        {
            return new List<Link>
            {
                new Link("F95Zone", Link)
            };
        }

        public override IEnumerable<MetadataNameProperty> GetDevelopers()
        {
            return new List<MetadataNameProperty> { Developer };
        }

        public override IEnumerable<MetadataNameProperty> GetPublishers()
        {
            return new List<MetadataNameProperty> { Developer };
        }

        public override int GetCommunityScore()
        {
            //F95 ratings go from 0.0 to 5.0 (Star based)
            //Playnite ratings go from 0 to 100
            return (int)(Rating / 5.0 * 100);
        }

        public override IEnumerable<MetadataNameProperty> GetAgeRatings()
        {
            return new List<MetadataNameProperty> { new MetadataNameProperty(AgeRatingAdult) };
        }
        
        #region Not Implemented

        public override int GetCriticScore()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<MetadataNameProperty> GetFeatures()
        {
            throw new NotImplementedException();
        }

        public override MetadataFile GetIcon()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<MetadataNameProperty> GetSeries()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<MetadataNameProperty> GetPlatforms()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<MetadataNameProperty> GetRegions()
        {
            throw new NotImplementedException();
        }
        
        #endregion
    }
}
