using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace VNDBMetadata
{
    public class VNDBMetadata : MetadataPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        public ILogger GetLogger => Logger;

        private VNDBMetadataSettings Settings { get; }

        public override Guid Id { get; } = Guid.Parse("613c7bba-36e9-437f-858a-3a8478cd489c");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Description
        };

        public override string Name => "VNDB";

        public VNDBMetadata(IPlayniteAPI api) : base(api)
        {
            Settings = new VNDBMetadataSettings(this);
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new VNDBMetadataProvider(options, this);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new VNDBMetadataSettingsView();
        }
    }
}