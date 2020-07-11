using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace F95ZoneMetadata
{
    public class F95ZoneMetadata : MetadataPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private F95ZoneMetadataSettings Settings { get; }

        public override Guid Id { get; } = Guid.Parse("a21b4484-858d-4786-9ce9-de17b3628ef2");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Description
        };

        public override string Name => "F95Zone";

        public F95ZoneMetadata(IPlayniteAPI api) : base(api)
        {
            Settings = new F95ZoneMetadataSettings(this);
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new F95ZoneMetadataProvider(options, this);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new F95ZoneMetadataSettingsView();
        }
    }
}