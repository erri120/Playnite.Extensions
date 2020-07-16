using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Common;
using Playnite.SDK;
using Playnite.SDK.Metadata;
using Playnite.SDK.Models;

namespace F95ZoneMetadata
{
    public class F95ZoneMetadataProvider : OnDemandMetadataProvider
    {
        private readonly MetadataRequestOptions _options;
        private readonly F95ZoneMetadata _plugin;
        private ILogger Logger => _plugin.GetLogger;

        private List<MetadataField> _availableFields;
        public override List<MetadataField> AvailableFields => _availableFields ?? (_availableFields = GetAvailableFields());

        private F95ZoneGame _game;

        public F95ZoneMetadataProvider(MetadataRequestOptions options, F95ZoneMetadata plugin)
        {
            _options = options;
            _plugin = plugin;
        }

        private List<MetadataField> GetAvailableFields()
        {
            var game = _options.GameData;
            var list = new List<MetadataField>();

            var name = game.Name;

            if (name.IsEmpty())
            {
                var link = game.Links.FirstOrDefault(x =>
                    x.Name.Equals("F95Zone", StringComparison.InvariantCultureIgnoreCase));
                if (link == null)
                    return list;
                name = link.Url;
            }

            if (!name.StartsWith(F95ZoneGame.Root))
            {
                var link = game.Links.FirstOrDefault(x =>
                    x.Name.Equals("F95Zone", StringComparison.InvariantCultureIgnoreCase));
                if (link == null)
                    return list;
                name = link.Url;
            }

            _game = F95ZoneGame.LoadGame(name, Logger).Result;
            if(_game == null)
            {
                throw new Exception($"Game for {name} is null!");
            }

            list.Add(MetadataField.Links);

            if (!_game.Name.IsEmpty())
                list.Add(MetadataField.Name);

            if (!_game.Developer.IsEmpty())
                list.Add(MetadataField.Developers);

            if (!_game.Overview.IsEmpty())
                list.Add(MetadataField.Description);

            if (_game.Genres != null && _game.Genres.Count > 0)
                list.Add(MetadataField.Genres);

            if(_game.LabelList != null && _game.LabelList.Count > 0)
                list.Add(MetadataField.Tags);

            if(!_game.CoverImageURL.IsEmpty())
                list.Add(MetadataField.CoverImage);

            if(_game.PreviewImageURLs != null && _game.PreviewImageURLs.Count > 0)
                list.Add(MetadataField.BackgroundImage);

            return list;
        }

        public override string GetName()
        {
            return AvailableFields.Contains(MetadataField.Name)
                ? _game.Name
                : base.GetName();
        }

        public override List<string> GetDevelopers()
        {
            return AvailableFields.Contains(MetadataField.Developers)
                ? new List<string> {_game.Developer}
                : base.GetDevelopers();
        }

        public override string GetDescription()
        {
            return AvailableFields.Contains(MetadataField.Description)
                ? _game.Overview
                : base.GetDescription();
        }

        public override List<string> GetGenres()
        {
            return AvailableFields.Contains(MetadataField.Genres)
                ? _game.Genres
                : base.GetGenres();
        }

        public override List<string> GetTags()
        {
            return AvailableFields.Contains(MetadataField.Tags)
                ? _game.LabelList
                : base.GetTags();
        }

        public override List<Link> GetLinks()
        {
            return AvailableFields.Contains(MetadataField.Links)
                ? new List<Link> { new Link("F95Zone", _game.F95Link) }
                : base.GetLinks();
        }

        public override MetadataFile GetCoverImage()
        {
            if (!AvailableFields.Contains(MetadataField.CoverImage))
                return base.GetCoverImage();

            var file = new MetadataFile(_game.CoverImageURL);
            return file;
        }

        public override MetadataFile GetBackgroundImage()
        {
            if (!AvailableFields.Contains(MetadataField.BackgroundImage))
                return base.GetBackgroundImage();

            List<ImageFileOption> options = _game.PreviewImageURLs
                .Select(x => new ImageFileOption(x))
                .ToList();

            var option = _plugin.PlayniteApi.Dialogs.ChooseImageFile(options, "Select Background Image");
            if (option == null)
                return base.GetBackgroundImage();

            var file = new MetadataFile(option.Path);
            return file;
        }
    }
}