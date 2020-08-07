using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Common;
using Playnite.SDK;
using Playnite.SDK.Metadata;
using Playnite.SDK.Models;

namespace DLSiteMetadata
{
    public class DLSiteGame : AGame
    {
        public override string Name { get; set; }
        public override string Description { get; set; }
        public override string Link { get; set; }

        public Link Circle { get; set; }
        public List<string> ImageURLs { get; set; }
        public DateTime Release { get; set; }
        public List<DLSiteGenre> Genres { get; set; }

        private MetadataFile GetImage(bool coverImage)
        {
            List<ImageFileOption> options = ImageURLs
                .Select(x => new ImageFileOption(x))
                .ToList();

            var option = PlayniteAPI.Dialogs.ChooseImageFile(options, coverImage ? "Select Cover Image" : "Select Background Image");
            if (option == null)
                return null;

            var file = new MetadataFile(option.Path);
            return file;
        }

        public override MetadataFile GetCoverImage()
        {
            return GetImage(true);
        }

        public override MetadataFile GetBackgroundImage()
        {
            return GetImage(false);
        }

        public override DateTime GetReleaseDate()
        {
            return Release;
        }

        public override List<string> GetGenres()
        {
            return Genres.Select(x => x.ENG).ToList();
        }

        public override List<Link> GetLinks()
        {
            return new List<Link>
            {
                new Link("DLSite", Link),
                Circle
            };
        }

        public override List<string> GetDevelopers()
        {
            return new List<string> { Circle.Name };
        }

        public override List<string> GetPublishers()
        {
            return GetDevelopers();
        }

        #region Not Implemented

        public override int GetCommunityScore()
        {
            throw new NotImplementedException();
        }

        public override int GetCriticScore()
        {
            throw new NotImplementedException();
        }

        public override List<string> GetFeatures()
        {
            throw new NotImplementedException();
        }

        public override MetadataFile GetIcon()
        {
            throw new NotImplementedException();
        }

        public override List<string> GetTags()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
