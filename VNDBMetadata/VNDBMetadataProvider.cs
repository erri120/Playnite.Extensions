using System;
using Playnite.SDK.Plugins;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Extensions.Common;
using Playnite.SDK;
using Playnite.SDK.Metadata;
using Playnite.SDK.Models;

namespace VNDBMetadata
{
    public class VNDBMetadataProvider : OnDemandMetadataProvider
    {
        private readonly MetadataRequestOptions _options;
        private readonly VNDBMetadata _plugin;
        private ILogger Logger => _plugin.GetLogger;

        private List<MetadataField> _availableFields;
        public override List<MetadataField> AvailableFields => _availableFields ?? (_availableFields = GetAvailableFields());

        private readonly VNDBClient _client;
        private VisualNovel _visualNovel;

        public VNDBMetadataProvider(MetadataRequestOptions options, VNDBMetadata plugin)
        {
            _options = options;
            _plugin = plugin;

            _client = new VNDBClient(StaticSettings.UseTLS);
            var res = _client.Login().Result;
            if(!res)
                throw new Exception("Login failed!");
        }

        private List<MetadataField> GetAvailableFields()
        {
            var game = _options.GameData;
            var list = new List<MetadataField>();

            if (game.Name.IsEmpty())
            {
                Logger.Warn("Tried to get VNDB metadata but the name is empty!");
                return list;
            }

            var name = game.Name;

            if (name.StartsWith("v"))
                name = name.Substring(1);

            if (name.StartsWith("https://vndb.org/v"))
                name = name.Replace("https://vndb.org/v", "");

            //either ID or a name for search
            var doSearch = !int.TryParse(name, out var id);

            Result<VisualNovel> res = doSearch
                ? _client.SearchVN(name).Result 
                : _client.GetVNByID(id).Result;

            if (res == null || res.items.Count == 0)
            {
                Logger.Warn("Found no VNs in VNDB!");
                return list;
            }

            if (res.items.Count > 1)
            {
                List<GenericItemOption> options = res.items
                    .Select(x => new GenericItemOption(x.title, x.description))
                    .ToList();
                
                var item = _plugin.PlayniteApi.Dialogs.ChooseItemWithSearch(options, s =>
                {
                    if (s == name)
                        return options;
                    Result<VisualNovel> results = _client.SearchVN(s).Result;
                    return results.items
                        .Select(x => new GenericItemOption(x.title, x.description))
                        .ToList();
                }, name, "Select Visual Novel");

                if (item == null)
                    return list;

                res = _client.GetVNByTitle(item.Name).Result;
                _visualNovel = res.items.First();
            }
            else
            {
                if(doSearch)
                    res = _client.GetVNByTitle(name).Result;
                _visualNovel = res.items.First();
            }

            list.Add(MetadataField.Links);

            if (!_visualNovel.title.IsEmpty())
                list.Add(MetadataField.Name);
            if (!_visualNovel.description.IsEmpty())
                list.Add(MetadataField.Description);
            if (!_visualNovel.image.IsEmpty())
                list.Add(MetadataField.CoverImage);
            if (Math.Abs(_visualNovel.rating) > double.Epsilon)
                list.Add(MetadataField.CommunityScore);
            if (_visualNovel.screens != null && _visualNovel.screens.Count > 0)
                list.Add(MetadataField.BackgroundImage);
            if (!_visualNovel.released.IsEmpty())
                list.Add(MetadataField.ReleaseDate);
            if (_visualNovel.tags != null && _visualNovel.tags.Count > 0)
                list.Add(MetadataField.Genres);

            return list;
        }

        public override string GetName()
        {
            return AvailableFields.Contains(MetadataField.Name)
                ? _visualNovel.title
                : base.GetName();
        }

        private static string ToHTML(string vndbDescription)
        {
            var sb = new StringBuilder();
            
            //weird things happen here, don't ask or try to understand it

            var startingTag = false;
            var endingTag = false;
            var startingURL = false;
            var finishedStartingTag = false;
            var urlLink = new StringBuilder();
            var urlName = new StringBuilder();
            var finishedURL = false;

            for (var i = 0; i < vndbDescription.Length; i++)
            {
                var cur = vndbDescription[i];

                //from
                //[url=http://en.wikipedia.org/wiki/Saya_no_Uta]Wikipedia[/url]
                //to
                //<a href="http://en.wikipedia.org/wiki/Saya_no_Uta">Wikipedia</a>
                if (cur == '[')
                {
                    if (i != vndbDescription.Length - 1)
                    {
                        var next = vndbDescription[i + 1];
                        if (next == 'u' || next == 'U')
                        {
                            startingTag = true;
                            continue;
                        }
                    }
                }

                if (startingTag && !startingURL)
                {
                    if (cur == '=')
                    {
                        startingURL = true;
                    }
                    continue;
                }

                if (startingURL && cur != ']')
                {
                    urlLink.Append(cur);
                    continue;
                }

                if (cur == ']' && startingURL)
                {
                    startingURL = false;
                    startingTag = false;
                    finishedStartingTag = true;
                    continue;
                }

                if (finishedStartingTag && cur != '[')
                {
                    urlName.Append(cur);
                    continue;
                }

                if (finishedStartingTag && cur == '[')
                {
                    if (i != vndbDescription.Length - 1)
                    {
                        var next = vndbDescription[i + 1];
                        if (next == '/')
                        {
                            endingTag = true;
                            finishedStartingTag = false;
                            continue;
                        }
                    }
                }

                if (endingTag && cur != ']')
                {
                    continue;
                }

                if (endingTag && cur == ']')
                {
                    finishedURL = true;
                }

                if (finishedURL)
                {
                    sb.Append($"<a href=\"{urlLink}\">{urlName}</a>");
                    startingTag = false;
                    endingTag = false;
                    urlLink.Clear();
                    urlName.Clear();
                    finishedURL = false;
                    continue;
                }

                sb.Append(cur);
            }

            var result = sb.ToString();
            return result;
        }

        public override string GetDescription()
        {
            return AvailableFields.Contains(MetadataField.Description)
                ? ToHTML(_visualNovel.description)
                : base.GetDescription();
        }

        public override MetadataFile GetCoverImage()
        {
            if (!AvailableFields.Contains(MetadataField.CoverImage))
                return base.GetCoverImage();

            var file = new MetadataFile(_visualNovel.image);
            return file;
        }

        public override MetadataFile GetBackgroundImage()
        {
            if (!AvailableFields.Contains(MetadataField.BackgroundImage))
                return base.GetBackgroundImage();

            if (_visualNovel.screens.Count == 1)
            {
                var file = new MetadataFile(_visualNovel.screens.First().image);
                return file;
            }

            List<ImageFileOption> options = _visualNovel.screens
                .Select(x => new ImageFileOption(x.image))
                .ToList();
            
            var image = _plugin.PlayniteApi.Dialogs.ChooseImageFile(options, "Select a Background Image");
            return new MetadataFile(image.Path);
        }

        public override List<Link> GetLinks()
        {
            if (!AvailableFields.Contains(MetadataField.Links))
                return base.GetLinks();

            var list = new List<Link>
            {
                new Link("VNDB", $"https://vndb.org/v{_visualNovel.id}")
            };

            if (_visualNovel.links == null) return list;

            if(!_visualNovel.links.wikidata.IsEmpty())
                list.Add(new Link("Wikidata", $"https://www.wikidata.org/wiki/{_visualNovel.links.wikidata}"));
            if(!_visualNovel.links.wikipedia.IsEmpty())
                list.Add(new Link("Wikipedia", $"https://wikipedia.org/wiki/{_visualNovel.links.wikipedia}"));
            /*if(!_visualNovel.links.encubed.IsEmpty())
                    list.Add(new Link("Encubed", $"{_visualNovel.links.encubed}"));*/
            if(!_visualNovel.links.renai.IsEmpty())
                list.Add(new Link("Ren'Ai", $"https://renai.us/game/{_visualNovel.links.renai}"));

            return list;
        }

        public override int? GetCommunityScore()
        {
            if (!AvailableFields.Contains(MetadataField.CommunityScore))
                return base.GetCommunityScore();

            return (int) (_visualNovel.rating*10);
        }

        public override DateTime? GetReleaseDate()
        {
            if (!AvailableFields.Contains(MetadataField.ReleaseDate))
                return base.GetReleaseDate();

            return !DateTime.TryParse(_visualNovel.released, out var date) 
                ? base.GetReleaseDate() 
                : date;
        }

        public override List<string> GetGenres()
        {
            if (!AvailableFields.Contains(MetadataField.Genres))
                return base.GetGenres();

            List<BasicTag> tags = _visualNovel.tags.Select(x =>
            {
                var id = (int) x[0];
                var tag = VNDBTags.GetTagByID(id);
                return tag;
            }).NotNull().OrderByDescending(x => x.vns).ToList();

            return tags.Take(StaticSettings.MaxTags).Select(x => x.name).ToList();
        }

        public override void Dispose()
        {
            _client.Dispose();
        }
    }
}