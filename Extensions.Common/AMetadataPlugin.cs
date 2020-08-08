using System;
using System.Collections.Generic;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace Extensions.Common
{
    public abstract class AMetadataPlugin<T> : MetadataPlugin where T : AGame, new()
    {
        public ILogger Logger { get; } = LogManager.GetLogger();

        protected AMetadataPlugin(IPlayniteAPI playniteAPI) : base(playniteAPI)
        {
        }

        protected abstract AMetadataProvider<T> GetProvider();

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            AMetadataProvider<T> provider = GetProvider();
            provider.Options = options;
            provider.Plugin = this;

            return provider;
        }
    }
}
