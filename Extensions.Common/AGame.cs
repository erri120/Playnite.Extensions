using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace Extensions.Common
{
    public abstract class AGame
    {
        public ILogger Logger { get; set; }
        public string ID { get; set; }

        protected AGame() { }

        protected AGame(ILogger logger, string id)
        {
            Logger = logger;
            ID = id;
        }

        public abstract Task<AGame> LoadGame();

        public readonly List<MetadataField> AvailableFields = new List<MetadataField> {MetadataField.Links};

        public abstract string Name { get; set; }
        public virtual string GetName()
        {
            return Name;
        }

        public abstract string Description { get; set; }
        public virtual string GetDescription()
        {
            return Description;
        }

        public abstract string Link { get; set; }

        public abstract MetadataFile GetCoverImage(IDialogsFactory dialogsAPI);
        public abstract MetadataFile GetBackgroundImage(IDialogsFactory dialogsAPI);
        public abstract ReleaseDate GetReleaseDate();
        public abstract IEnumerable<MetadataNameProperty> GetGenres();
        public abstract int GetCommunityScore();
        public abstract List<Link> GetLinks();
        public abstract int GetCriticScore();
        public abstract IEnumerable<MetadataNameProperty> GetDevelopers();
        public abstract IEnumerable<MetadataNameProperty> GetFeatures();
        public abstract MetadataFile GetIcon();
        public abstract IEnumerable<MetadataNameProperty> GetPublishers();
        public abstract IEnumerable<MetadataNameProperty> GetTags();
        public abstract IEnumerable<MetadataNameProperty> GetAgeRatings();
        public abstract IEnumerable<MetadataNameProperty> GetSeries();
        public abstract IEnumerable<MetadataNameProperty> GetPlatforms();
        public abstract IEnumerable<MetadataNameProperty> GetRegions();

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

        protected void LogFound(string name, [CanBeNull] string value)
        {
            Logger.Debug(value == null ? $"Found {name} for {ID}" : $"Found {name} \"{value}\" for {ID}");
        }

        protected void LogFound<T>(string name, IList<T> value)
        {
            Logger.Debug($"Found {value.Count} {name} for {ID}");
        }

        public const string AgeRatingAdult = "Adult";
    }
}
