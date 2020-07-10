using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace DLSiteMetadata
{
    public class DLSiteMetadata : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private DLSiteMetadataSettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("6bff51a5-ad52-4474-af76-f8e410b66e20");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Description
            // Include addition fields if supported by the metadata source
        };

        // Change to something more appropriate
        public override string Name => "Custom Metadata";

        public DLSiteMetadata(IPlayniteAPI api) : base(api)
        {
            settings = new DLSiteMetadataSettings(this);
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new DLSiteMetadataProvider(options, this);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new DLSiteMetadataSettingsView();
        }
    }
}