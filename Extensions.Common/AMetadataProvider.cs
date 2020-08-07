using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Playnite.SDK;
using Playnite.SDK.Metadata;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace Extensions.Common
{
    public abstract class AMetadataProvider<TGame> : OnDemandMetadataProvider where TGame : AGame
    {
        public AMetadataPlugin<TGame> Plugin { get; set; }
        public MetadataRequestOptions Options { get; set; }

        private bool _isTestMode;
        private ILogger TestLogger { get; set; }

        protected ILogger Logger => _isTestMode ? TestLogger : Plugin.Logger;
        private TGame Game { get; set; }

        public abstract Task<TGame> LoadGame();
        protected abstract string GetID();

        private string _id;
        protected string ID
        {
            get
            {
                if (_id != null)
                    return _id;

                _id = GetID();
                return _id;
            }
        }

        public void SetTestMode(ILogger testLogger, string id)
        {
            Plugin = new DummyPlugin<TGame>();
            Options = new MetadataRequestOptions(new Game(id), false);
            _isTestMode = true;
            TestLogger = testLogger;
        }

        protected bool TryGetInnerText(HtmlNode baseNode, string xpath, string name, out string innerText)
        {
            return baseNode.TryGetInnerText(xpath, Logger, name, ID, out innerText);
        }

        protected bool IsNullOrEmpty(HtmlNodeCollection collection, string name)
        {
            return collection.IsNullOrEmpty(Logger, name, ID);
        }

        protected bool IsNull(HtmlNode node, string name)
        {
            return node.IsNull(Logger, name, ID);
        }

        protected bool IsEmpty(string s, string name)
        {
            return s.IsEmpty(Logger, name, ID);
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
                    Game = LoadGame().Result;
                }
                catch (Exception e)
                {
                    throw new LoadingGameException(ID, e);
                }

                return Game.AvailableFields;
            }
        }

        public override MetadataFile GetBackgroundImage()
        {
            return AvailableFields.Contains(MetadataField.BackgroundImage)
                ? Game.GetBackgroundImage()
                : base.GetBackgroundImage();
        }

        public override DateTime? GetReleaseDate()
        {
            return AvailableFields.Contains(MetadataField.ReleaseDate)
                ? Game.GetReleaseDate()
                : base.GetReleaseDate();
        }

        public override MetadataFile GetCoverImage()
        {
            return AvailableFields.Contains(MetadataField.CoverImage)
                ? Game.GetCoverImage()
                : base.GetCoverImage();
        }

        public override string GetName()
        {
            return AvailableFields.Contains(MetadataField.Name)
                ? Game.GetName()
                : base.GetName();
        }

        public override List<string> GetGenres()
        {
            return AvailableFields.Contains(MetadataField.Genres)
                ? Game.GetGenres()
                : base.GetGenres();
        }

        public override string GetDescription()
        {
            return AvailableFields.Contains(MetadataField.Description)
                ? Game.GetDescription()
                : base.GetDescription();
        }

        public override int? GetCommunityScore()
        {
            return AvailableFields.Contains(MetadataField.CommunityScore)
                ? Game.GetCommunityScore()
                : base.GetCommunityScore();
        }

        public override List<Link> GetLinks()
        {
            return AvailableFields.Contains(MetadataField.Links)
                ? Game.GetLinks()
                : base.GetLinks();
        }

        public override int? GetCriticScore()
        {
            return AvailableFields.Contains(MetadataField.CriticScore)
                ? Game.GetCriticScore()
                : base.GetCriticScore();
        }

        public override List<string> GetDevelopers()
        {
            return AvailableFields.Contains(MetadataField.Developers)
                ? Game.GetDevelopers()
                : base.GetDevelopers();
        }

        public override List<string> GetFeatures()
        {
            return AvailableFields.Contains(MetadataField.Features)
                ? Game.GetFeatures()
                : base.GetFeatures();
        }

        public override MetadataFile GetIcon()
        {
            return AvailableFields.Contains(MetadataField.Icon)
                ? Game.GetIcon()
                : base.GetIcon();
        }

        public override List<string> GetPublishers()
        {
            return AvailableFields.Contains(MetadataField.Publishers)
                ? Game.GetPublishers()
                : base.GetPublishers();
        }

        public override List<string> GetTags()
        {
            return AvailableFields.Contains(MetadataField.Tags)
                ? Game.GetTags()
                : base.GetTags();
        }

        #endregion
    }
}
