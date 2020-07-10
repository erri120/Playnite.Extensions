using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;

namespace DLSiteMetadata
{
    public class DLSiteMetadataProvider : OnDemandMetadataProvider
    {
        private readonly MetadataRequestOptions options;
        private readonly DLSiteMetadata plugin;

        public override List<MetadataField> AvailableFields => throw new NotImplementedException();

        public DLSiteMetadataProvider(MetadataRequestOptions options, DLSiteMetadata plugin)
        {
            this.options = options;
            this.plugin = plugin;
        }

        // Override additional methods based on supported metadata fields.
        public override string GetDescription()
        {
            return options.GameData.Name + " description";
        }
    }
}