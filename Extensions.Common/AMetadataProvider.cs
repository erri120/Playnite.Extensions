using System;
using System.Collections.Generic;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace Extensions.Common
{
    public abstract class AMetadataProvider<TGame> : OnDemandMetadataProvider where TGame : AGame, new()
    {
        public AMetadataPlugin<TGame> Plugin { get; set; }
        public MetadataRequestOptions Options { get; set; }

        private AGame Game { get; set; }

        protected abstract string GetID();

        private string _id;
        private string ID
        {
            get
            {
                if (_id != null)
                    return _id;

                _id = GetID();
                return _id;
            }
        }

        #region Overrides

        public override List<MetadataField> AvailableFields
        {
            get
            {
                if (Game != null)
                    return Game.AvailableFields;

                try
                {
                    var game = new TGame
                    {
                        ID = ID,
                        Logger = Plugin.Logger
                    };
                    var res = game.LoadGame().Result;
                    Game = res;
                }
                catch (Exception e)
                {
                    throw new LoadingGameException(ID, e);
                }

                return Game.AvailableFields;
            }
        }

        public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.BackgroundImage)
                ? Game.GetBackgroundImage(Plugin.PlayniteApi.Dialogs)
                : base.GetBackgroundImage(args);
        }

        public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.ReleaseDate)
                ? Game.GetReleaseDate()
                : base.GetReleaseDate(args);
        }

        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.CoverImage)
                ? Game.GetCoverImage(Plugin.PlayniteApi.Dialogs)
                : base.GetCoverImage(args);
        }

        public override string GetName(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.Name)
                ? Game.GetName()
                : base.GetName(args);
        }

        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.Genres)
                ? Game.GetGenres()
                : base.GetGenres(args);
        }

        public override string GetDescription(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.Description)
                ? Game.GetDescription()
                : base.GetDescription(args);
        }

        public override int? GetCommunityScore(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.CommunityScore)
                ? Game.GetCommunityScore()
                : base.GetCommunityScore(args);
        }

        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.Links)
                ? Game.GetLinks()
                : base.GetLinks(args);
        }

        public override int? GetCriticScore(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.CriticScore)
                ? Game.GetCriticScore()
                : base.GetCriticScore(args);
        }

        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.Developers)
                ? Game.GetDevelopers()
                : base.GetDevelopers(args);
        }

        public override IEnumerable<MetadataProperty> GetFeatures(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.Features)
                ? Game.GetFeatures()
                : base.GetFeatures(args);
        }

        public override MetadataFile GetIcon(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.Icon)
                ? Game.GetIcon()
                : base.GetIcon(args);
        }

        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.Publishers)
                ? Game.GetPublishers()
                : base.GetPublishers(args);
        }

        public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.Tags)
                ? Game.GetTags()
                : base.GetTags(args);
        }

        public override IEnumerable<MetadataProperty> GetAgeRatings(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.AgeRating)
                ? Game.GetAgeRatings()
                : base.GetAgeRatings(args);
        }

        public override IEnumerable<MetadataProperty> GetSeries(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.Series)
                ? Game.GetSeries()
                : base.GetSeries(args);
        }

        public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.Platform)
                ? Game.GetPlatforms()
                : base.GetPlatforms(args);
        }

        public override IEnumerable<MetadataProperty> GetRegions(GetMetadataFieldArgs args)
        {
            return AvailableFields.Contains(MetadataField.Region)
                ? Game.GetRegions()
                : base.GetRegions(args);
        }

        #endregion
    }
}
