using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;

namespace F95ZoneMetadata
{
    public class F95ZoneMetadataProvider : OnDemandMetadataProvider
    {
        private readonly MetadataRequestOptions _options;
        private readonly F95ZoneMetadata _plugin;

        public override List<MetadataField> AvailableFields => throw new NotImplementedException();

        public F95ZoneMetadataProvider(MetadataRequestOptions options, F95ZoneMetadata plugin)
        {
            _options = options;
            _plugin = plugin;
        }

        // Override additional methods based on supported metadata fields.
        public override string GetDescription()
        {
            return _options.GameData.Name + " description";
        }
    }
}