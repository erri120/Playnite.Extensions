using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Extensions.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace FanzaMetadata;

[UsedImplicitly]
public class FanzaMetadataPlugin : MetadataPlugin
{
    private readonly IPlayniteAPI _playniteAPI;
    private readonly ILogger<FanzaMetadataPlugin> _logger;
    private readonly Settings _settings;

    public override string Name => "Fanza";
    public override Guid Id => Guid.Parse("efc848be-82e1-4e3d-a151-59e5fab3e39a");

    public override List<MetadataField> SupportedFields { get; } = new();

    public FanzaMetadataPlugin(IPlayniteAPI playniteAPI) : base(playniteAPI)
    {
        _playniteAPI = playniteAPI;
        _logger = CustomLogger.GetLogger<FanzaMetadataPlugin>(nameof(FanzaMetadataPlugin));

        _settings = new Settings(this, playniteAPI);
    }

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        return new FanzaMetadataProvider(_playniteAPI, _settings, options);
    }

    public override ISettings GetSettings(bool firstRunSettings) => _settings;

    public override UserControl GetSettingsView(bool firstRunView)
    {
        return new SettingsView();
    }
}
