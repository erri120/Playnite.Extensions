using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Extensions.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;

namespace F95ZoneMetadata;

[UsedImplicitly]
public class F95ZoneMetadataPlugin : MetadataPlugin
{
    private readonly IPlayniteAPI _playniteAPI;
    private readonly ILogger<F95ZoneMetadataPlugin> _logger;
    private readonly Settings _settings;
    private readonly UpdateTracking _updateTracking;

    private readonly CancellationTokenSource _cts = new();

    public override string Name => "F95zone";
    public override Guid Id => Guid.Parse("3af84c02-7825-4cd6-b0bd-d0800d26ffc5");

    public override List<MetadataField> SupportedFields { get; } = new();

    public F95ZoneMetadataPlugin(IPlayniteAPI playniteAPI) : base(playniteAPI)
    {
        _playniteAPI = playniteAPI;
        _logger = CustomLogger.GetLogger<F95ZoneMetadataPlugin>(nameof(F95ZoneMetadataPlugin));

        _settings = new Settings(this, _playniteAPI);

        var updateFile = Path.Combine(GetPluginUserDataPath(), "updates.json");
        _updateTracking = File.Exists(updateFile)
            ? Serialization.FromJsonFile<UpdateTracking>(updateFile)
            : new UpdateTracking();
    }

    public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
    {
        var toRemove = _updateTracking.Games.Where(tracking => !_playniteAPI.Database.Games.Any(game => game.Id.Equals(tracking.GameId))).ToList();
        _logger.LogDebug("Removing {Count} games from tracking because they no longer exist in the database", toRemove.Count);

        foreach (var item in toRemove)
        {
            _updateTracking.Games.Remove(item);
        }

        File.WriteAllText(Path.Combine(GetPluginUserDataPath(), "updates.json"), Serialization.ToJson(_updateTracking, true));
        if (!_settings.CheckForUpdates) return;

        var f95Games = _playniteAPI.Database.Games
            .Where(game => game.Links is not null && game.Links.Any(link => link.Name is not null && link.Name.Equals("F95zone", StringComparison.OrdinalIgnoreCase)))
            .Select(game => (game, tracking: _updateTracking.GetOrAdd(game)))
            .Where(tuple => tuple.tracking.NeedsUpdate(tuple.game, _settings))
            .ToList();

        _logger.LogInformation("Looking for updates for {Count} games", f95Games.Count);

        var task = Task.Run(async () =>
        {
            var scrapper = F95ZoneMetadataProvider.SetupScrapper(_settings);

            // TODO: run in parallel
            foreach (var tuple in f95Games)
            {
                var (game, tracking) = tuple;
                tracking.LastChecked = DateTime.UtcNow;

                var id = F95ZoneMetadataProvider.GetIdFromGame(game);
                if (id is null)
                {
                    _logger.LogError("Unable to get Id for game {Game}", game.Name);
                    continue;
                }

                var res = await scrapper.ScrapPage(id, _cts.Token);
                if (res is null)
                {
                    _logger.LogWarning("Unable to scrap page with Id {Id}", id);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(res.Version)) continue;
                if (string.Equals(game.Version, res.Version, StringComparison.OrdinalIgnoreCase)) continue;

                _playniteAPI.Notifications.Add(new NotificationMessage(Guid.NewGuid().ToString(), $"{game.Name} has updated from \"{game.Version}\" to \"{res.Version}\"", NotificationType.Info,
                    () =>
                    {
                        Process.Start("explorer.exe", $"{Scrapper.DefaultBaseUrl}{id}");
                    }));
            }

            _logger.LogInformation("Finished looking for updates");
            File.WriteAllText(Path.Combine(GetPluginUserDataPath(), "updates.json"), Serialization.ToJson(_updateTracking, true));
        }, _cts.Token);
    }

    public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
    {
        _cts.Cancel();
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

    public override void Dispose()
    {
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }

        _cts.Dispose();
    }
}
