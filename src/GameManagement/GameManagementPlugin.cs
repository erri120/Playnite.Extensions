using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Extensions.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Other;
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
        _logger = CustomLogger.GetLogger<GameManagementPlugin>(nameof(GameManagementPlugin));

        AssemblyLoader.ValidateReferencedAssemblies(_logger);

        _storageInfo = new StorageInfo(_playniteAPI);
        _storageInfo.LoadFromFile(StoragePath);
    }

    public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
    {
        yield return new GameMenuItem
        {
            Action = UninstallGameMenuAction,
            Description = "Uninstall"
        };

        yield return new GameMenuItem
        {
            Action = UninstallAndRemoveGameMenuAction,
            Description = "Uninstall and Remove"
        };
    }

    private void UninstallGameMenuAction(GameMenuItemActionArgs args)
    {
        UninstallGames(args);
    }

    private void UninstallAndRemoveGameMenuAction(GameMenuItemActionArgs args)
    {
        var games = UninstallGames(args);
        foreach (var game in games)
        {
            _playniteAPI.Database.Games.Remove(game);
        }
    }

    private List<Game> UninstallGames(GameMenuItemActionArgs args)
    {
        var games = args.Games;
        if (games is null || !games.Any()) return new List<Game>();

        var result = _playniteAPI.Dialogs.ShowMessage($"Do you really want to uninstall {games.Count} game(s)?",
            "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return new List<Game>();
        }

        _logger.LogInformation("Uninstalling {Count} game(s)", games.Count.ToString());

        var actuallyUninstalledGames = new List<Game>(games.Count);

        _playniteAPI.Dialogs.ActivateGlobalProgress(progressArgs =>
        {
            progressArgs.ProgressMaxValue = games.Count;
            progressArgs.CurrentProgressValue = 0;

            foreach (var game in games)
            {
                if (progressArgs.CancelToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Uninstallation has been canceled");
                    return;
                }

                _logger.LogDebug("Uninstalling {Name}", game.Name);

                progressArgs.CurrentProgressValue += 1;
                progressArgs.Text = $"Uninstalling {game.Name}";

                if (game.InstallationStatus != InstallationStatus.Installed
                    || string.IsNullOrWhiteSpace(game.InstallDirectory)
                    || !Directory.Exists(game.InstallDirectory))
                {
                    _logger.LogError("Game {Name} is not installed!", game.Name);
                    continue;
                }

                Directory.Delete(game.InstallDirectory, true);
                game.IsInstalled = false;
                actuallyUninstalledGames.Add(game);
                _storageInfo.RemoveStorageInfo(game);
            }

            _storageInfo.SaveToFile(StoragePath);
        }, new GlobalProgressOptions($"Uninstalling {games.Count} game(s)", true));

        return actuallyUninstalledGames;
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
