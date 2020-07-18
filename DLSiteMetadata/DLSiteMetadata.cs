using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DLSiteMetadata
{
    public class DLSiteMetadata : MetadataPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        internal ILogger GetLogger => Logger;

        private DLSiteMetadataSettings Settings { get; }

        public override Guid Id { get; } = Guid.Parse("6bff51a5-ad52-4474-af76-f8e410b66e20");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.Description,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.BackgroundImage,
            MetadataField.CoverImage,
            //MetadataField.CommunityScore,
            MetadataField.Genres,
            //MetadataField.Icon,
            MetadataField.Links,
            MetadataField.ReleaseDate,
            // Include addition fields if supported by the metadata source
        };

        public override string Name => "DLSite";

        public DLSiteMetadata(IPlayniteAPI api) : base(api)
        {
            Settings = new DLSiteMetadataSettings(this);

            Task.Run(() =>
            {
                var dataDir = Path.Combine(api.Paths.ExtensionsDataPath, Id.ToString());
                var count = DLSiteGenres.LoadGenres(dataDir);
                Logger.Info($"Loaded {count} DLSite genres");
            });
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new DLSiteMetadataProvider(options, this);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new DLSiteMetadataSettingsView();
        }
    }
}