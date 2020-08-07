using System;
using System.Collections.Generic;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace Extensions.Common
{
    public abstract class AMetadataPlugin<T> : MetadataPlugin where T : AGame
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

    public class DummyPlugin<TGame> : AMetadataPlugin<TGame> 
        where TGame : AGame
    {
        public DummyPlugin() : base(null)
        {
        }

        public override Guid Id => Guid.Empty;
        public override string Name => "Dummy Plugin";
        public override List<MetadataField> SupportedFields { get; }
        protected override AMetadataProvider<TGame> GetProvider()
        {
            throw new NotImplementedException();
        }
    }
}
