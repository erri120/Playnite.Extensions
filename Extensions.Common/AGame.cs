using System;
using System.Collections.Generic;
using Playnite.SDK;
using Playnite.SDK.Metadata;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace Extensions.Common
{
    public abstract class AGame
    {
        public IPlayniteAPI PlayniteAPI { get; set; }

        public readonly List<MetadataField> AvailableFields = new List<MetadataField>();

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

        public abstract MetadataFile GetBackgroundImage();
        public abstract DateTime GetReleaseDate();
        public abstract MetadataFile GetCoverImage();
        public abstract List<string> GetGenres();
        public abstract int GetCommunityScore();
        public abstract List<Link> GetLinks();
        public abstract int GetCriticScore();
        public abstract List<string> GetDevelopers();
        public abstract List<string> GetFeatures();
        public abstract MetadataFile GetIcon();
        public abstract List<string> GetPublishers();
        public abstract List<string> GetTags();
    }
}
