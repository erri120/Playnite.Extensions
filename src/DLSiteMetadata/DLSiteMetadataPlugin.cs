using System;
using System.Collections.Generic;
using System.Windows.Controls;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using Extensions.Common;
using Microsoft.Extensions.Logging;
using Other;

namespace DLSiteMetadata;

[UsedImplicitly]
public class DLSiteMetadataPlugin : MetadataPlugin
{
    private readonly IPlayniteAPI _playniteAPI;
    private readonly ILogger<DLSiteMetadataPlugin> _logger;
    private readonly Settings _settings;

    public override string Name => "DLsite";
    public override Guid Id => Guid.Parse("7fa7b951-3d32-4844-a274-468e1adf8cca");

    public override List<MetadataField> SupportedFields => Fields;
    public static readonly List<MetadataField> Fields = new()
    {
        MetadataField.Name,
        MetadataField.Developers,
        MetadataField.Features,
        MetadataField.Genres,
        MetadataField.Icon,
        MetadataField.Links,
        MetadataField.Tags,
        MetadataField.BackgroundImage,
        MetadataField.CoverImage,
        MetadataField.ReleaseDate,
        MetadataField.Series,
        MetadataField.Description,
        MetadataField.Region
    };

    public DLSiteMetadataPlugin(IPlayniteAPI playniteAPI) : base(playniteAPI)
    {
        _playniteAPI = playniteAPI;
        _logger = CustomLogger.GetLogger<DLSiteMetadataPlugin>(nameof(DLSiteMetadataPlugin));

        AssemblyLoader.ValidateReferencedAssemblies(_logger);

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
