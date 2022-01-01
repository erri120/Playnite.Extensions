using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Extensions.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace F95ZoneMetadata;

[UsedImplicitly]
public class F95ZoneMetadataPlugin : MetadataPlugin
{
    private readonly IPlayniteAPI _playniteAPI;
    private readonly ILogger<F95ZoneMetadataPlugin> _logger;
    private readonly Settings _settings;

    public override string Name => "F95zone";
    public override Guid Id => Guid.Parse("3af84c02-7825-4cd6-b0bd-d0800d26ffc5");

    public override List<MetadataField> SupportedFields { get; } = new();

    public F95ZoneMetadataPlugin(IPlayniteAPI playniteAPI) : base(playniteAPI)
    {
        _playniteAPI = playniteAPI;
        _logger = CustomLogger.GetLogger<F95ZoneMetadataPlugin>(nameof(F95ZoneMetadataPlugin));

        _settings = new Settings(this, _playniteAPI);
    }

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        return new F95ZoneMetadataProvider(_playniteAPI, _settings, options);
    }

    public override ISettings GetSettings(bool firstRunSettings) => _settings;

    public override UserControl GetSettingsView(bool firstRunView)
    {
        return new SettingsView();
    }
}
