using System;
using Playnite.SDK.Plugins;
using System.Collections.Generic;
using System.Linq;
using Extensions.Common;
using Playnite.SDK;
using Playnite.SDK.Metadata;
using Playnite.SDK.Models;

namespace DLSiteMetadata
{
    public class DLSiteMetadataProvider : OnDemandMetadataProvider
    {
        private readonly MetadataRequestOptions _options;
        private readonly DLSiteMetadata _plugin;
        private ILogger Logger => _plugin.GetLogger;

        private List<MetadataField> _availableFields;
        public override List<MetadataField> AvailableFields => _availableFields ?? (_availableFields = GetAvailableFields());

        private DLSiteGame _game;

        public DLSiteMetadataProvider(MetadataRequestOptions options, DLSiteMetadata plugin)
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
                var dlSiteLink = game.Links.FirstOrDefault(x =>
                    x.Name.Equals("DLSite", StringComparison.InvariantCultureIgnoreCase));
                if(dlSiteLink == null)
                    return list;
                name = dlSiteLink.Url;
            }

            var id = name;

            IDCheck:
            if (!id.StartsWith("RJ", StringComparison.InvariantCultureIgnoreCase)
                && !id.StartsWith("RE", StringComparison.InvariantCultureIgnoreCase))
            {
                if (id.StartsWith(Consts.RootENG) || id.StartsWith(Consts.RootJPN))
                {
                    //https://www.dlsite.com/ecchi-eng/work/=/product_id/RE234198.html
                    var root = id.StartsWith(Consts.RootENG) ? Consts.RootENG : Consts.RootJPN;
                    id = id.Replace(root, "");
                    //work/=/product_id/{id}.html
                    id = id.Replace(".html", "").Replace("work/=/product_id/", "");
                    goto IDCheck;
                }

                var dlSiteLink = game.Links.FirstOrDefault(x =>
                    x.Name.Equals("DLSite", StringComparison.InvariantCultureIgnoreCase));

                if (dlSiteLink != null)
                {
                    id = dlSiteLink.Url;
                    goto IDCheck;
                }

                Logger.Warn($"Trying to get metadata for {id} but it does not start with RJ/RE!");
                return list;
            }

            _game = DLSiteGame.LoadGame(id, Logger).Result;

            list.Add(MetadataField.Links);

            if(!_game.Name.IsEmpty())
                list.Add(MetadataField.Name);
            
            if(!_game.Circle.IsEmpty())
            {
                list.Add(MetadataField.Developers);
                list.Add(MetadataField.Publishers);
            }

            if (!_game.Description.IsEmpty())
                list.Add(MetadataField.Description);

            if(!_game.Release.IsEmpty())
                list.Add(MetadataField.ReleaseDate);

            if(_game.Genres != null && _game.Genres.Count > 0)
                list.Add(MetadataField.Genres);

            if(_game.ImageURLs != null && _game.ImageURLs.Count > 0)
            {
                list.Add(MetadataField.CoverImage);
                if(_game.ImageURLs.Count > 1)
                    list.Add(MetadataField.BackgroundImage);
            }

            //TODO: Age Ratings
            //TODO: Platform

            return list;
        }

        public override string GetName()
        {
            return !AvailableFields.Contains(MetadataField.Name) 
                ? base.GetName()
                : _game.Name;
        }

        public override List<string> GetDevelopers()
        {
            return !AvailableFields.Contains(MetadataField.Developers)
                ? base.GetDevelopers()
                : new List<string> {_game.Circle};
        }

        public override List<string> GetPublishers()
        {
            return !AvailableFields.Contains(MetadataField.Publishers)
                ? base.GetPublishers()
                : new List<string> { _game.Circle };
        }

        public override string GetDescription()
        {
            return !AvailableFields.Contains(MetadataField.Description)
                ? base.GetDescription()
                : _game.Description;
        }

        public override DateTime? GetReleaseDate()
        {
            if (!AvailableFields.Contains(MetadataField.ReleaseDate))
                return base.GetReleaseDate();

            return DateTime.TryParse(_game.Release, out var date) 
                ? date 
                : base.GetReleaseDate();
        }

        public override List<string> GetGenres()
        {
            return !AvailableFields.Contains(MetadataField.Genres) 
                ? base.GetGenres()
                : _game.Genres;
        }

        public override MetadataFile GetCoverImage()
        {
            if (!AvailableFields.Contains(MetadataField.CoverImage)) return base.GetCoverImage();

            List<ImageFileOption> options = _game.ImageURLs
                .Select(x => new ImageFileOption(x))
                .ToList();

            var option = _plugin.PlayniteApi.Dialogs.ChooseImageFile(options, "Select Cover Image");
            if (option == null)
                return base.GetCoverImage();

            var file = new MetadataFile(option.Path);
            return file;
        }

        public override MetadataFile GetBackgroundImage()
        {
            if (!AvailableFields.Contains(MetadataField.BackgroundImage)) return base.GetBackgroundImage();

            List<ImageFileOption> options = _game.ImageURLs
                .Select(x => new ImageFileOption(x))
                .ToList();

            var option = _plugin.PlayniteApi.Dialogs.ChooseImageFile(options, "Select Background Image");
            if (option == null)
                return base.GetBackgroundImage();
            
            var file = new MetadataFile(option.Path);
            return file;
        }

        public override List<Link> GetLinks()
        {
            if (!AvailableFields.Contains(MetadataField.Links)) return base.GetLinks();

            var list = new List<Link>
            {
                new Link("DLSite", _game.DLSiteLink)
            };

            if(!_game.CircleLink.IsEmpty() && !_game.Circle.IsEmpty())
                list.Add(new Link(_game.Circle, _game.CircleLink));

            return list;
        }
    }
}