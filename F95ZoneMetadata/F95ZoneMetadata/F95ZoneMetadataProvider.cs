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

            if (game.Name.IsEmpty())
                return list;

            if (!game.Name.StartsWith(F95ZoneGame.Root))
            {
                Logger.Warn($"Tried to get metadata for {game.Name} but it does not start with {F95ZoneGame.Root}!");
                return list;
            }

            _game = F95ZoneGame.LoadGame(game.Name, Logger).Result;

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

            var url = _game.PreviewImageURLs.First();
            var file = new MetadataFile(url);
            return file;
        }
    }
}