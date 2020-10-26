using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using JetBrains.Annotations;

namespace VNDBMetadata
{
    [UsedImplicitly]
    public class VNDBMetadata : MetadataPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        public ILogger GetLogger => Logger;

        internal VNDBMetadataSettings Settings { get; }

        public override Guid Id { get; } = Guid.Parse("613c7bba-36e9-437f-858a-3a8478cd489c");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.Description,
            MetadataField.CoverImage,
            MetadataField.BackgroundImage,
            MetadataField.ReleaseDate,
            MetadataField.CommunityScore,
            MetadataField.Genres,
            MetadataField.Links
        };

        public override string Name => "VNDB";

        public VNDBMetadata(IPlayniteAPI api) : base(api)
        {
            Settings = new VNDBMetadataSettings(this);

            Task.Run(async () =>
            {
                var dataDir = GetPluginUserDataPath();
                var res = await VNDBTags.GetLatestDumb(dataDir);
                if (!res)
                {
                    Logger.Error("Unable to get latest tags dumb!");
                    return;
                }

                var count = VNDBTags.ReadTags(dataDir);
                Logger.Info($"Tags cache contains {count} tags");
            });
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