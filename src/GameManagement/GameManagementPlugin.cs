using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extensions.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace GameManagement;

[UsedImplicitly]
public class GameManagementPlugin : GenericPlugin
{
    private readonly IPlayniteAPI _playniteAPI;
    private readonly StorageInfo _storageInfo;
    private readonly ILogger<GameManagementPlugin> _logger;

    private string StoragePath => Path.Combine(GetPluginUserDataPath(), "storage.json");

    public override Guid Id => Guid.Parse("a37e0963-91ac-4432-be2a-69e366c44726");

    public GameManagementPlugin(IPlayniteAPI playniteAPI) : base(playniteAPI)
    {
        _playniteAPI = playniteAPI;

        _storageInfo = new StorageInfo(_playniteAPI);
        _storageInfo.LoadFromFile(StoragePath);

        _logger = CustomLogger.GetLogger<GameManagementPlugin>(nameof(GameManagementPlugin));
    }

    public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
    {
        yield return new GameMenuItem
        {
            Action = UninstallGameMenuAction,
            Description = "Uninstall"
        };
    }

    private void UninstallGameMenuAction(GameMenuItemActionArgs args)
    {
        var games = args.Games;
        if (games is null || !games.Any()) return;

        _logger.LogInformation("Uninstalling {Count} games", games.Count.ToString());

        foreach (var game in games)
        {
            _logger.LogDebug("Uninstalling {Name}", game.Name);

            if (game.InstallationStatus != InstallationStatus.Installed
                || string.IsNullOrWhiteSpace(game.InstallDirectory)
                || !Directory.Exists(game.InstallDirectory))
            {
                _logger.LogError("Game {Name} is not installed!", game.Name);
                continue;
            }

            Directory.Delete(game.InstallDirectory, true);
            _playniteAPI.Database.Games.Remove(game);

            // the internal uninstallation thing by Playnite is unreliable and is intended to remove the files but not
            // the game from Playnite itself
            // _playniteAPI.UninstallGame(game.Id);

            _storageInfo.RemoveStorageInfo(game);
        }

        _storageInfo.SaveToFile(StoragePath);
    }

    private readonly CancellationTokenSource _source = new();

    public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
    {
        Task.Run(() =>
        {
            _storageInfo.UpdateStorageInfoForAllNewGames();
            _storageInfo.SaveToFile(StoragePath);
        }, _source.Token);
    }

    public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
    {
        if (_source.IsCancellationRequested) return;
        _source.Cancel();
        _source.Dispose();
    }

    public override void OnGameInstalled(OnGameInstalledEventArgs args)
    {
        _storageInfo.AddStorageInfo(args.Game);
        _storageInfo.SaveToFile(StoragePath);
    }

    public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
    {
        _storageInfo.RemoveStorageInfo(args.Game);
        _storageInfo.SaveToFile(StoragePath);
    }

    public override IEnumerable<SidebarItem> GetSidebarItems()
    {
        yield return new SidebarItem
        {
            Title = "View Storage Statistics",
            Type = SiderbarItemType.View,
            Visible = true,
            Opened = () => new StorageStatisticsView
            {
                DataContext = _storageInfo
            }
        };
    }
}
