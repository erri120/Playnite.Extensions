using System;
using System.Collections.Generic;
using System.Windows.Controls;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using Extensions.Common;
using Microsoft.Extensions.Logging;

namespace DLSiteMetadata;

[UsedImplicitly]
public class DLSiteMetadataPlugin : MetadataPlugin
{
    private readonly IPlayniteAPI _playniteAPI;
    private readonly ILogger<DLSiteMetadataPlugin> _logger;
    private readonly Settings _settings;

    public override string Name => "DLsite";
    public override Guid Id => Guid.Parse("7fa7b951-3d32-4844-a274-468e1adf8cca");

    public override List<MetadataField> SupportedFields { get; } = new();

    public DLSiteMetadataPlugin(IPlayniteAPI playniteAPI) : base(playniteAPI)
    {
        _playniteAPI = playniteAPI;
        _logger = CustomLogger.GetLogger<DLSiteMetadataPlugin>(nameof(DLSiteMetadataPlugin));

        _settings = new Settings(_playniteAPI, this);
    }

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        return new DLSiteMetadataProvider(_playniteAPI, _settings, options);
    }

    public override ISettings GetSettings(bool firstRunSettings) => _settings;

    public override UserControl GetSettingsView(bool firstRunView)
    {
        return new SettingsView();
    }
}
