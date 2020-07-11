using Playnite.SDK.Plugins;
using System.Collections.Generic;
using Extensions.Common;
using Playnite.SDK;

namespace VNDBMetadata
{
    public class VNDBMetadataProvider : OnDemandMetadataProvider
    {
        private readonly MetadataRequestOptions _options;
        private readonly VNDBMetadata _plugin;
        private ILogger Logger => _plugin.GetLogger;

        private List<MetadataField> _availableFields;
        public override List<MetadataField> AvailableFields => _availableFields ?? (_availableFields = GetAvailableFields());

        public VNDBMetadataProvider(MetadataRequestOptions options, VNDBMetadata plugin)
        {
            _options = options;
            _plugin = plugin;
        }

        private List<MetadataField> GetAvailableFields()
        {
            var game = _options.GameData;
            var list = new List<MetadataField>();

            if (game.Name.IsEmpty())
                return list;



            return list;
        }

        public override string GetDescription()
        {
            return _options.GameData.Name + " description";
        }
    }
}